import os
import re
import sys

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

def extract_endpoint_calls(filepath, content):
    calls = []
    map_pattern = re.compile(r"\.(MapGet|MapPost|MapPut|MapDelete|MapPatch)\s*\(\s*\"([^\"]+)\"")
    feature_patterns = [
        re.compile(r"new\s+([A-Za-z0-9_]+Feature)\.(Query|Command)"),
        re.compile(r"new\s+([A-Za-z0-9_]+)\.(Query|Command)"),
        re.compile(r"([A-Za-z0-9_]+Feature)\.(Command|Query)\s+\w+"),
        re.compile(r"([A-Za-z0-9_]+)\.(Command|Query)\s+\w+"),
    ]
    for m in map_pattern.finditer(content):
        method = m.group(1)
        route = m.group(2)
        start = m.end()
        snippet = content[start:start+1200]
        feature_name = None
        feature_type = None
        for pat in feature_patterns:
            fm = pat.search(snippet)
            if fm:
                feature_name = fm.group(1)
                feature_type = fm.group(2)
                break
        calls.append({
            "method": method,
            "route": route,
            "feature_name": feature_name,
            "feature_type": feature_type,
            "file": os.path.basename(filepath)
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
        has_cmd = re.search(r"record\s+Command|class\s+Command|struct\s+Command", content)
        has_qry = re.search(r"record\s+Query|class\s+Query|struct\s+Query", content)
        has_hdl = re.search(r"class\s+Handler|record\s+Handler", content)
        if (has_cmd or has_qry) and has_hdl:
            handlers.append({
                "name": rel,
                "file": f,
                "has_command": bool(has_cmd),
                "has_query": bool(has_qry)
            })
    return handlers

def analyze_module(module, base_path):
    api_root = None
    app_root = None
    mod_path = os.path.join(base_path, "src", "modules", module)
    if not os.path.isdir(mod_path):
        return None
    for dp, dn, fn in os.walk(mod_path):
        for d in dn:
            if d.endswith(".API") and api_root is None:
                api_root = os.path.join(dp, d)
            if d.endswith(".Application") and app_root is None:
                app_root = os.path.join(dp, d)
    result = {
        "module": module,
        "endpoints": [],
        "handlers": [],
        "orphan_endpoints": [],
        "dead_handlers": [],
        "graphql_issues": [],
        "http_issues": []
    }
    if api_root and os.path.isdir(api_root):
        api_files = find_cs_files(api_root)
        for f in api_files:
            try:
                content = open(f, "r", encoding="utf-8", errors="ignore").read()
            except Exception:
                continue
            calls = extract_endpoint_calls(f, content)
            result["endpoints"].extend(calls)
    if app_root and os.path.isdir(app_root):
        app_files = find_cs_files(app_root)
        result["handlers"] = extract_handlers(app_files)
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
        print("Endpoints found:", len(res["endpoints"]))
        for ep in res['endpoints']:
            print("  [" + ep["method"] + "] " + ep["route"] + " -> Feature: " + str(ep["feature_name"]) + " (" + str(ep["feature_type"]) + ") [file: " + ep["file"] + "]")
        print("Handlers found:", len(res["handlers"]))
        for h in res['handlers']:
            print("  Handler:", h["name"], "[cmd=" + str(h["has_command"]) + ", qry=" + str(h["has_query"]) + "]")

if __name__ == "__main__":
    main()
