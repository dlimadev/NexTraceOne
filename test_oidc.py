import re

content = open('src/modules/identityaccess/NexTraceOne.IdentityAccess.API/Endpoints/Endpoints/AuthEndpoints.cs', 'r', encoding='utf-8', errors='ignore').read()

def find_statement_end(content, start):
    i = start
    depth = 0
    in_string = False
    string_char = None
    while i < len(content):
        ch = content[i]
        if in_string:
            if ch == string_char and (i == 0 or content[i-1] != chr(92)):
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

for m in re.finditer(r'\.(MapGet|MapPost|MapPut|MapDelete|MapPatch)\s*\(\s*"([^"]+)"', content):
    method = m.group(1)
    route = m.group(2)
    if 'oidc' not in route and 'saml' not in route:
        continue
    start = m.end()
    end = find_statement_end(content, start)
    snippet = content[start:end]
    print(method, route, 'len:', len(snippet))
    feature = None
    ftype = None
    for pat in [
        re.compile(r'new\s+([A-Za-z0-9_]+Feature)\.(Query|Command)\s*\('),
        re.compile(r'new\s+([A-Za-z0-9_]+)\.(Query|Command)\s*\('),
    ]:
        fm = pat.search(snippet)
        if fm:
            feature = fm.group(1)
            ftype = fm.group(2)
            break
    print('  feature:', feature, 'type:', ftype)
