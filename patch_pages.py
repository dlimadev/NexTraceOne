import re

PAGES_DIR = 'src/frontend/src/features/change-governance/pages'
IMPORT_LINE = "import { useEnvironment } from '../../../contexts/EnvironmentContext';"
ENV_CONST = "  const { activeEnvironmentId } = useEnvironment();"

def patch_file(path, add_env_after, query_key_replacements, skip_lines_containing=None):
    with open(path, 'r') as f:
        content = f.read()

    # Add import if not present
    if 'useEnvironment' not in content:
        # Insert import before the first blank line or after last import
        lines = content.split('\n')
        last_import = -1
        for i, line in enumerate(lines):
            if line.startswith('import '):
                last_import = i
        if last_import >= 0:
            lines.insert(last_import + 1, IMPORT_LINE)
            content = '\n'.join(lines)

    # Add const if not present
    if 'activeEnvironmentId' not in content:
        # Insert after the marker
        content = content.replace(add_env_after, add_env_after + '\n' + ENV_CONST, 1)

    # Replace queryKeys (only in non-invalidation contexts)
    for old, new in query_key_replacements:
        content = content.replace(old, new, 1)

    with open(path, 'w') as f:
        f.write(content)
    print(f'Patched: {path}')

