import os
import re
import sys
from collections import defaultdict

MODULES = [
    "aiknowledge", "auditcompliance", "catalog", "changegovernance",
    "configuration", "governance", "identityaccess", "integrations",
    "knowledge", "notifications", "operationalintelligence", "productanalytics"
]

def find_cs_files(root, exclude_obj_bin=True):
    files = []
    for dp, dn, fn in os.walk(root):
        if exclude_obj_bin:
            norm = dp.replace(os.sep, '/')
            if '/obj/' in norm or '/bin/' in norm:
                continue
        for f in fn:
            if f.endswith('.cs'):
                files.append(os.path.join(dp, f))
    return files

def extract_using_aliases(content):
    aliases = {}
    for m in re.finditer(r'using\s+([A-Za-z0-9_]+)\s*=\s*([^;]+);', content):
        alias = m.group(1)
        full = m.group(2).strip()
        aliases[alias] = full
    return aliases

def extract_endpoint_calls(filepath, content):
    calls = []
    aliases = extract_using_aliases(content)
    map_pattern = re.compile(r'\.(MapGet|MapPost|MapPut|MapDelete|MapPatch)\s*\(\s*"([^"]+)"', re.IGNORECASE)
    feature_patterns = [
        re.compile(r'new\s+([A-Za-z0-9_]+Feature)\.(Query|Command)\s*\('),
        re.compile(r'new\s+([A-Za-z0-9_]+)\.(Query|Command)\s*\('),
        re.compile(r'new\s+([A-Za-z0-9_]+Feature)\.(Query|Command)'),
        re.compile(r'new\s+([A-Za-z0-9_]+)\.(Query|Command)'),
        re.compile(r'([A-Za-z0-9_]+Feature)\.(Command|Query)\s+\w+'),
        re.compile(r'([A-Za-z0-9_]+)\.(Command|Query)\s+\w+'),
    ]
    for m in map_pattern.finditer(content):
        method = m.group(1)
        route = m.group(2)
        start = m.end()
        end_brace = content.find(');', start)
        if end_brace == -1:
            end_brace = content.find(')', start)
        if end_brace == -1:
            end_brace = start + 1500
        else:
            end_brace += 1
        snippet = content[start:end_brace]
        feature_name = None
        feature_type = None
        for pat in feature_patterns:
            fm = pat.search(snippet)
            if fm:
                feature_name = fm.group(1)
                feature_type = fm.group(2)
                break
        if feature_name and feature_name.endswith('Feature') and feature_name in aliases:
            resolved = aliases[feature_name]
            feature_name = resolved.split('.')[-1]
        elif feature_name and feature_name.endswith('Feature'):
            feature_name = feature_name[:-7]
        calls.append({
            "method": method,
            "route": route,
            "feature_name": feature_name,
            "feature_type": feature_type,
            "file": os.path.basename(filepath),
            "has_body": "Command" in (feature_type or "")
        })
    return calls

def extract_handlers(app_files):
    handlers = []
    for f in app_files:
        try:
            content = open(f, "r", encoding="utf-8", errors="ignore").read()
        except Exception:
            continue
        rel = os.path.basename(f).replace('.cs', '')
        has_cmd = bool(re.search(r'record\s+Command|class\s+Command|struct\s+Command', content))
        has_qry = bool(re.search(r'record\s+Query|class\s+Query|struct\s+Query', content))
        has_hdl = bool(re.search(r'class\s+Handler|record\s+Handler', content))
        if (has_cmd or has_qry) and has_hdl:
            handlers.append({
                "name": rel,
                "file": f,
                "has_command": has_cmd,
                "has_query": has_qry
            })
    return handlers

def find_graphql_files(api_files):
    gql = []
    for f in api_files:
        base = os.path.basename(f)
        if 'GraphQL' in f or base.endswith('Query.cs') or base.endswith('Mutation.cs') or base.endswith('Subscription.cs'):
            gql.append(f)
    return gql

def analyze_module(module, base_path):
    api_root = None
    app_root = None
    mod_path = os.path.join(base_path, "src", "modules", module)
    if not os.path.isdir(mod_path):
        return None
    for dp, dn, fn in os.walk(mod_path):
        for d in dn:
            if d.endswith('.API') and api_root is None:
                api_root = os.path.join(dp, d)
            if d.endswith('.Application') and app_root is None:
                app_root = os.path.join(dp, d)
    result = {
        "module": module,
        "endpoints": [],
        "handlers": [],
        "orphan_endpoints": [],
        "dead_handlers": [],
        "graphql_issues": [],
        "http_issues": [],
        "gql_files": []
    }
    api_files = []
    if api_root and os.path.isdir(api_root):
        api_files = find_cs_files(api_root)
        for f in api_files:
            try:
                content = open(f, "r", encoding="utf-8", errors="ignore").read()
            except Exception:
                continue
            calls = extract_endpoint_calls(f, content)
            result["endpoints"].extend(calls)
        result["gql_files"] = find_graphql_files(api_files)
    handler_files = []
    if app_root and os.path.isdir(app_root):
        app_files = find_cs_files(app_root)
        result["handlers"] = extract_handlers(app_files)
        handler_files = [h["file"] for h in result["handlers"]]
    
    handler_names = set(h["name"] for h in result["handlers"])
    for ep in result["endpoints"]:
        if ep["feature_name"] and ep["feature_name"] not in handler_names:
            result["orphan_endpoints"].append(ep)
        if ep["method"] == "MapGet" and ep["has_body"]:
            result["http_issues"].append((ep, "GET endpoint with Command body"))
        if ep["method"] in ("MapPost", "MapPut", "MapPatch") and not ep["has_body"] and ep["feature_type"] == "Query":
            result["http_issues"].append((ep, "POST/PUT/PATCH endpoint with Query (no body)"))
    
    referenced = set()
    for ep in result["endpoints"]:
        if ep["feature_name"]:
            referenced.add(ep["feature_name"])
    for h in result["handlers"]:
        if h["name"] not in referenced:
            result["dead_handlers"].append(h)
    
    return result

def main():
    base = sys.argv[1] if len(sys.argv) > 1 else "."
    for mod in MODULES:
        res = analyze_module(mod, base)
        if not res:
            continue
        print()
        print("=" * 60)
        print("MODULE:", mod)
        print("=" * 60)
        print("Endpoints:", len(res["endpoints"]))
        print("Handlers:", len(res["handlers"]))
        print("Orphan endpoints (no handler):", len(res["orphan_endpoints"]))
        for ep in res["orphan_endpoints"]:
            print("  ORPHAN [" + ep["method"] + "] " + ep["route"] + " -> " + str(ep["feature_name"]) + " [" + ep["file"] + "]")
        print("Dead handlers (no endpoint):", len(res["dead_handlers"]))
        for h in res["dead_handlers"][:20]:
            print("  DEAD   " + h["name"] + " [" + h["file"].replace(base, "").replace(os.sep, "/") + "]")
        if len(res["dead_handlers"]) > 20:
            print("  ... and " + str(len(res["dead_handlers"]) - 20) + " more")
        print("HTTP issues:", len(res["http_issues"]))
        for ep, issue in res["http_issues"][:10]:
            print("  HTTP   [" + ep["method"] + "] " + ep["route"] + " -> " + str(ep["feature_name"]) + " [" + issue + "]")
        if len(res["http_issues"]) > 10:
            print("  ... and " + str(len(res["http_issues"]) - 10) + " more")
        print("GraphQL files:", len(res["gql_files"]))
        for g in res["gql_files"]:
            print("  GQL    " + g.replace(base, "").replace(os.sep, "/"))

if __name__ == "__main__":
    main()
