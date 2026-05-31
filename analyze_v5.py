import os
import re
import sys

MODULES = [
    "aiknowledge", "auditcompliance", "catalog", "changegovernance",
    "configuration", "governance", "identityaccess", "integrations",
    "knowledge", "notifications", "operationalintelligence", "productanalytics"
]

def find_statement_end(content, paren_pos):
    i = paren_pos + 1
    depth = 1
    in_string = False
    string_char = None
    while i < len(content):
        ch = content[i]
        if in_string:
            if ch == string_char and (i == 0 or content[i-1] != ''):
                in_string = False
            i += 1
            continue
        if ch in (chr(34), chr(39)):
            in_string = True
            string_char = ch
            i += 1
            continue
        if ch == '(':
            depth += 1
        elif ch == ')':
            depth -= 1
            if depth == 0:
                j = i + 1
                while j < len(content) and content[j].isspace():
                    j += 1
                if j < len(content) and content[j] == '.':
                    i = j
                    depth = 0
                    continue
                if j < len(content) and content[j] == ';':
                    return j + 1
        i += 1
    return len(content)

def find_cs_files(root):
    files = []
    for dp, dn, fn in os.walk(root):
        if '/obj/' in dp.replace(os.sep, '/') or '/bin/' in dp.replace(os.sep, '/'):
            continue
        for f in fn:
            if f.endswith('.cs'):
                files.append(os.path.join(dp, f))
    return files

def extract_handler_classes(filepath):
    try:
        content = open(filepath, 'r', encoding='utf-8', errors='ignore').read()
    except:
        return []
    results = []
    for m in re.finditer(r'public\s+static\s+(?:class|record)\s+([A-Za-z0-9_]+)', content):
        name = m.group(1)
        start = m.start()
        end = content.find('public static class ', start+1)
        if end == -1:
            end = content.find('public static record ', start+1)
        if end == -1:
            end = len(content)
        block = content[start:end]
        has_cmd = bool(re.search(r'record\s+Command|class\s+Command', block))
        has_qry = bool(re.search(r'record\s+Query|class\s+Query', block))
        has_hdl = bool(re.search(r'class\s+Handler|record\s+Handler', block))
        if (has_cmd or has_qry) and has_hdl:
            results.append({'name': name, 'file': filepath, 'has_command': has_cmd, 'has_query': has_qry})
    if not results:
        base = os.path.basename(filepath).replace('.cs', '')
        has_cmd = bool(re.search(r'record\s+Command|class\s+Command', content))
        has_qry = bool(re.search(r'record\s+Query|class\s+Query', content))
        has_hdl = bool(re.search(r'class\s+Handler|record\s+Handler', content))
        if (has_cmd or has_qry) and has_hdl:
            results.append({'name': base, 'file': filepath, 'has_command': has_cmd, 'has_query': has_qry})
    return results

def extract_aliases(content):
    aliases = {}
    for m in re.finditer(r'using\s+([A-Za-z0-9_]+)\s*=\s*([^;]+);', content):
        aliases[m.group(1)] = m.group(2).strip()
    return aliases

def extract_endpoints(filepath, handler_names):
    try:
        content = open(filepath, 'r', encoding='utf-8', errors='ignore').read()
    except:
        return []
    aliases = extract_aliases(content)
    calls = []
    for m in re.finditer(r'\.(MapGet|MapPost|MapPut|MapDelete|MapPatch)\s*\(\s*"([^"]+)"', content):
        method = m.group(1)
        route = m.group(2)
        paren_pos = content.find("(", m.start())
        end = find_statement_end(content, paren_pos)
        snippet = content[paren_pos+1:end]
        feature = None
        ftype = None
        for pat in [
            re.compile(r'new\s+([A-Za-z0-9_]+Feature)\.(Query|Command)\s*\('),
            re.compile(r'new\s+([A-Za-z0-9_]+)\.(Query|Command)\s*\('),
            re.compile(r'new\s+([A-Za-z0-9_]+Feature)\.(Query|Command)'),
            re.compile(r'new\s+([A-Za-z0-9_]+)\.(Query|Command)'),
            re.compile(r'([A-Za-z0-9_]+Feature)\.(Command|Query)\s+\w+'),
            re.compile(r'([A-Za-z0-9_]+)\.(Command|Query)\s+\w+'),
        ]:
            fm = pat.search(snippet)
            if fm:
                feature = fm.group(1)
                ftype = fm.group(2)
                break
        if feature and feature.endswith('Feature') and feature in aliases:
            feature = aliases[feature].split('.')[-1]
        elif feature and feature.endswith('Feature'):
            feature = feature[:-7]
        exists = feature in handler_names if feature else None
        calls.append({
            'method': method, 'route': route, 'feature_name': feature,
            'feature_type': ftype, 'file': os.path.basename(filepath),
            'has_body': 'Command' in (ftype or ''), 'handler_exists': exists
        })
    return calls

def has_resolvers(filepath):
    try:
        c = open(filepath, 'r', encoding='utf-8', errors='ignore').read()
    except:
        return False
    return any(re.search(p, c) for p in [
        r'Resolve\s*\(', r'AddQueryType', r'AddMutationType',
        r'AddSubscriptionType', r'\.Field\s*\(', r'\.Type\s*\<', r' descriptor\.'
    ])

def analyze(mod, base):
    api_root = None
    app_root = None
    mp = os.path.join(base, 'src', 'modules', mod)
    if not os.path.isdir(mp):
        return None
    for dp, dn, fn in os.walk(mp):
        for d in dn:
            if d.endswith('.API') and api_root is None:
                api_root = os.path.join(dp, d)
            if d.endswith('.Application') and app_root is None:
                app_root = os.path.join(dp, d)
    res = {'module': mod, 'endpoints': [], 'handlers': [], 'orphan': [], 'dead': [], 'gql': [], 'gql_issues': [], 'http': []}
    hnames = set()
    if app_root:
        for f in find_cs_files(app_root):
            for h in extract_handler_classes(f):
                res['handlers'].append(h)
                hnames.add(h['name'])
    if api_root:
        for f in find_cs_files(api_root):
            for ep in extract_endpoints(f, hnames):
                res['endpoints'].append(ep)
            parts = f.replace(os.sep, '/').split('/')
            if 'GraphQL' in parts:
                b = os.path.basename(f)
                if b.endswith('.cs') and not b.endswith('Extensions.cs'):
                    res['gql'].append(f)
                    if not has_resolvers(f):
                        res['gql_issues'].append(f)
    for ep in res['endpoints']:
        if ep['feature_name'] and ep['handler_exists'] is False:
            res['orphan'].append(ep)
        if ep['method'] == 'MapGet' and ep['has_body']:
            res['http'].append((ep, 'GET endpoint with Command body'))
        if ep['method'] in ('MapPost', 'MapPut', 'MapPatch') and ep['feature_type'] == 'Query':
            res['http'].append((ep, 'POST/PUT/PATCH endpoint with Query (no body)'))
    ref = set(ep['feature_name'] for ep in res['endpoints'] if ep['feature_name'])
    for h in res['handlers']:
        if h['name'] not in ref:
            res['dead'].append(h)
    return res

def main():
    base = sys.argv[1] if len(sys.argv) > 1 else '.'
    for mod in MODULES:
        r = analyze(mod, base)
        if not r:
            continue
        print()
        print('='*60)
        print('MODULE:', mod)
        print('='*60)
        print('Endpoints:', len(r['endpoints']))
        print('Handlers:', len(r['handlers']))
        print('Orphan endpoints:', len(r['orphan']))
        for ep in r['orphan']:
            print('  ORPHAN [' + ep['method'] + '] ' + ep['route'] + ' -> ' + str(ep['feature_name']) + ' [' + ep['file'] + ']')
        print('Dead handlers:', len(r['dead']))
        for h in r['dead'][:30]:
            print('  DEAD   ' + h['name'] + ' [' + h['file'].replace(base, '').replace(os.sep, '/') + ']')
        if len(r['dead']) > 30:
            print('  ... and', len(r['dead']) - 30, 'more')
        print('HTTP issues:', len(r['http']))
        for ep, issue in r['http'][:15]:
            print('  HTTP   [' + ep['method'] + '] ' + ep['route'] + ' -> ' + str(ep['feature_name']) + ' [' + issue + ']')
        if len(r['http']) > 15:
            print('  ... and', len(r['http']) - 15, 'more')
        print('GraphQL files:', len(r['gql']))
        print('GraphQL without resolvers:', len(r['gql_issues']))
        for g in r['gql_issues']:
            print('  GQL    ' + g.replace(base, '').replace(os.sep, '/'))

if __name__ == '__main__':
    main()
