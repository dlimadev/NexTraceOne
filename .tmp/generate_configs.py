#!/usr/bin/env python3
"""Generate IEntityTypeConfiguration<T> files for AIKnowledge domain entities."""

import os
import re
import json
from pathlib import Path
from collections import defaultdict

ROOT = Path("c:/Users/dlima/Documents/GitHub/NexTraceOne")
DOMAIN_ROOT = ROOT / "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Domain"
INFRA_ROOT = ROOT / "src/modules/aiknowledge/NexTraceOne.AIKnowledge.Infrastructure"
OUT_DIR = INFRA_ROOT / "Persistence/Configurations"

ENTITY_DIRS = [
    DOMAIN_ROOT / "Governance/Entities",
    DOMAIN_ROOT / "ExternalAI/Entities",
    DOMAIN_ROOT / "Orchestration/Entities",
]

ENUM_DIRS = [
    DOMAIN_ROOT / "Governance/Enums",
    DOMAIN_ROOT / "ExternalAI/Enums",
    DOMAIN_ROOT / "Orchestration/Enums",
]


def to_snake_case_plural(name: str) -> str:
    """Convert PascalCase entity name to snake_case plural table name with aik_ prefix."""
    # Handle acronyms like AI, IDE, NLP
    s = re.sub(r"([A-Z]+)([A-Z][a-z])", r"\1_\2", name)
    s = re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", s)
    s = s.lower()
    # Pluralize simple cases
    if s.endswith("y") and not s.endswith("ay") and not s.endswith("ey") and not s.endswith("oy") and not s.endswith("uy"):
        s = s[:-1] + "ies"
    elif s.endswith("s") or s.endswith("x") or s.endswith("z") or s.endswith("ch") or s.endswith("sh"):
        s += "es"
    else:
        s += "s"
    return f"aik_{s}"


def simple_plural(name: str) -> str:
    if name.endswith("y") and name[-2].lower() not in "aeiou":
        return name[:-1] + "ies"
    if name.endswith(("s", "x", "z", "ch", "sh")):
        return name + "es"
    return name + "s"


def discover_enums() -> set[str]:
    enums = set()
    for d in ENUM_DIRS:
        if not d.exists():
            continue
        for f in d.glob("*.cs"):
            text = f.read_text(encoding="utf-8")
            for m in re.finditer(r"public\s+enum\s+(\w+)", text):
                enums.add(m.group(1))
    return enums


def discover_entities():
    entities = {}
    for d in ENTITY_DIRS:
        if not d.exists():
            continue
        for f in d.glob("*.cs"):
            text = f.read_text(encoding="utf-8")
            # skip files that only contain records/value objects without entity class
            class_match = re.search(
                r"public\s+(?:sealed\s+)?class\s+(\w+)\s*:\s*(AuditableEntity|Entity|AggregateRoot)<(\w+)>",
                text,
            )
            if not class_match:
                continue
            class_name = class_match.group(1)
            base_type = class_match.group(2)
            id_type = class_match.group(3)
            namespace_match = re.search(r"namespace\s+([\w.]+)", text)
            namespace = namespace_match.group(1) if namespace_match else ""

            # Find typed id record constructor in this file
            id_constructor = "new"
            id_record_match = re.search(
                rf"public\s+sealed\s+record\s+{id_type}\(Guid\s+Value\)\s*:\s*TypedIdBase\(Value\)\s*\{{([^}}]*)\}}",
                text,
            )
            if id_record_match:
                body = id_record_match.group(1)
                if "From(Guid" in body:
                    id_constructor = "From"

            props = []
            # Simple property regex (public T Name { get; ... }
            for pm in re.finditer(
                r"public\s+([\w\?\[\]<>]+)\s+(\w+)\s*\{\s*get;",
                text,
            ):
                ptype = pm.group(1).strip()
                pname = pm.group(2)
                # skip inherited Id if defined in base
                if pname == "Id" and ptype == id_type:
                    continue
                props.append({"type": ptype, "name": pname})

            entities[class_name] = {
                "file": str(f.relative_to(ROOT)),
                "namespace": namespace,
                "base": base_type,
                "id_type": id_type,
                "id_constructor": id_constructor,
                "properties": props,
            }
    return entities


def is_guid(type_name: str) -> bool:
    return type_name in ("Guid", "Guid?")


def is_datetime(type_name: str) -> bool:
    return type_name in ("DateTimeOffset", "DateTimeOffset?")


def is_json_property(name: str, ptype: str) -> bool:
    return name.endswith("Json") or ptype in ("JsonDocument", "JsonElement?", "JsonElement")


def is_text_long(name: str, ptype: str) -> bool:
    if ptype not in ("string", "string?"):
        return False
    long_names = {
        "Content", "Description", "SystemPrompt", "Prompt", "Response", "Query",
        "Context", "InputSchema", "OutputSchema", "Objective", "Reasoning", "ErrorMessage",
        "Notes", "Summary", "Text", "Payload", "RawInput", "RawOutput", "RawRequest",
        "RawResponse", "Transcript", "Configuration", "Metadata", "SourceContent",
        "AllowedContexts", "BlockedModelIds", "AllowedModelIds", "EnvironmentRestrictions",
        "Schema", "Instructions", "Template", "Body", "Message", "UserMessage",
        "AssistantMessage", "GroundingSources", "ContextReferences", "Capabilities",
        "Tags", "Tools", "ToolDefinitions", "Examples", "ExpectedOutput", "Embedding",
        "Vector", "Reason", "RejectionReason", "Evidence", "Observation", "ActionPlan",
    }
    return name in long_names


def type_to_clr(ptype: str) -> str:
    """Map C# type in property declaration to a configuration-friendly type name."""
    if ptype.endswith("?"):
        return type_to_clr(ptype[:-1])
    return ptype


def config_for_property(prop: dict, enums: set[str], entity_id_types: set[str]) -> list[str]:
    ptype = prop["type"]
    pname = prop["name"]
    lines = []

    # Skip navigation collections handled separately
    if re.match(r"^(IReadOnlyList|List|IEnumerable|ICollection)<", ptype):
        return lines

    # Typed ID property
    base_type = ptype.replace("?", "")
    if base_type in entity_id_types and not pname.endswith("Id"):
        # navigation property? skip
        return lines

    if pname.endswith("Id") and base_type in entity_id_types:
        # FK property using typed id
        lines.append(f"        builder.Property(x => x.{pname}).IsRequired({str(not ptype.endswith('?')).lower()});")
        return lines

    # Enum
    if base_type in enums:
        req = ".IsRequired()" if not ptype.endswith("?") else ""
        lines.append(f"        builder.Property(x => x.{pname}).HasConversion<string>().HasMaxLength(50){req};")
        return lines

    # RowVersion
    if pname == "RowVersion" and ptype == "uint":
        lines.append(f"        builder.Property(x => x.{pname}).IsRowVersion();")
        return lines

    # JSON
    if is_json_property(pname, ptype):
        req = ".IsRequired()" if not ptype.endswith("?") else ""
        lines.append(f"        builder.Property(x => x.{pname}).HasColumnType(\"jsonb\"){req};")
        return lines

    # Timestamp
    if is_datetime(ptype):
        req = ".IsRequired()" if not ptype.endswith("?") else ""
        lines.append(f"        builder.Property(x => x.{pname}).HasColumnType(\"timestamp with time zone\"){req};")
        return lines

    # Text long
    if is_text_long(pname, ptype):
        req = ".IsRequired()" if not ptype.endswith("?") else ""
        lines.append(f"        builder.Property(x => x.{pname}).HasColumnType(\"text\"){req};")
        return lines

    # String default
    if ptype in ("string", "string?"):
        req = ".IsRequired()" if ptype == "string" else ""
        lines.append(f"        builder.Property(x => x.{pname}).HasMaxLength(500){req};")
        return lines

    # Guid
    if is_guid(ptype):
        req = ".IsRequired()" if not ptype.endswith("?") else ""
        lines.append(f"        builder.Property(x => x.{pname}){req};")
        return lines

    # Default
    req = ".IsRequired()" if not ptype.endswith("?") and ptype != "string" else ""
    lines.append(f"        builder.Property(x => x.{pname}){req};")
    return lines


def discover_collections(entities: dict) -> dict:
    """Discover collection navigation properties that should map to separate tables."""
    collections = defaultdict(list)
    for class_name, info in entities.items():
        for p in info["properties"]:
            m = re.match(r"^(?:IReadOnlyList|List|IEnumerable|ICollection)<(\w+)>", p["type"])
            if m:
                collections[class_name].append({"name": p["name"], "item_type": m.group(1)})
    return dict(collections)


def generate_configuration(class_name: str, info: dict, enums: set[str], entity_names: set[str], entity_id_types: set[str]) -> str:
    namespace = "NexTraceOne.AIKnowledge.Infrastructure.Persistence.Configurations"
    table = to_snake_case_plural(class_name)
    id_type = info["id_type"]
    id_ctor = info["id_constructor"]
    id_conversion = f"{id_ctor}(value)" if id_ctor != "new" else f"new {id_type}(value)"

    # Determine import namespace for entity
    entity_namespace = info["namespace"]

    lines = [
        "using Microsoft.EntityFrameworkCore;",
        "using Microsoft.EntityFrameworkCore.Metadata.Builders;",
        f"using {entity_namespace};",
        "using NexTraceOne.BuildingBlocks.Core.StronglyTypedIds;",
        "",
        f"namespace {namespace};",
        "",
        f"internal sealed class {class_name}Configuration : IEntityTypeConfiguration<{class_name}>",
        "{",
        f"    public void Configure(EntityTypeBuilder<{class_name}> builder)",
        "    {",
        f"        builder.ToTable(\"{table}\");",
        "        builder.HasKey(x => x.Id);",
        f"        builder.Property(x => x.Id).HasConversion(id => id.Value, value => {id_conversion});",
    ]

    # TenantId if exists
    has_tenant = any(p["name"] == "TenantId" for p in info["properties"])
    if has_tenant:
        lines.append("        builder.Property(x => x.TenantId).HasMaxLength(200).IsRequired();")

    for prop in info["properties"]:
        lines.extend(config_for_property(prop, enums, entity_id_types))

    # Foreign keys: properties ending in Id that are Guid and correspond to other entities
    fk_lines = []
    for prop in info["properties"]:
        pname = prop["name"]
        ptype = prop["type"]
        if pname == "TenantId":
            continue
        if pname == "Id":
            continue
        if not is_guid(ptype):
            continue
        # Try to find referenced entity
        ref_name = pname[:-2] if pname.endswith("Id") else None
        if ref_name and ref_name in entity_names:
            fk_lines.append(f"        builder.HasOne<{ref_name}>()")
            fk_lines.append(f"            .WithMany()")
            fk_lines.append(f"            .HasForeignKey(x => x.{pname})")
            fk_lines.append(f"            .OnDelete(DeleteBehavior.Restrict);")
    if fk_lines:
        lines.append("")
        lines.extend(fk_lines)

    # Indexes
    idx_props = []
    if has_tenant:
        idx_props.append("TenantId")
    for prop in info["properties"]:
        pname = prop["name"]
        ptype = prop["type"]
        if pname == "TenantId":
            continue
        if is_guid(ptype) and pname != "Id":
            idx_props.append(pname)
        if pname in ("Name", "Email", "Slug", "CorrelationId", "Status", "CreatedAt", "UpdatedAt"):
            idx_props.append(pname)
    if idx_props:
        lines.append("")
        for ip in idx_props:
            lines.append(f"        builder.HasIndex(x => x.{ip});")

    lines.append("    }")
    lines.append("}")
    return "\n".join(lines)


def main():
    enums = discover_enums()
    entities = discover_entities()
    entity_names = set(entities.keys())
    entity_id_types = {e["id_type"] for e in entities.values()}

    # Ensure output directory exists
    OUT_DIR.mkdir(parents=True, exist_ok=True)

    generated = []
    for class_name, info in sorted(entities.items()):
        config_text = generate_configuration(class_name, info, enums, entity_names, entity_id_types)
        file_name = OUT_DIR / f"{class_name}Configuration.cs"
        file_name.write_text(config_text + "\n", encoding="utf-8")
        generated.append(class_name)

    # Save metadata for review
    meta = {
        "generated": generated,
        "count": len(generated),
        "entities": {k: {**v, "properties": v["properties"]} for k, v in entities.items()},
    }
    (ROOT / ".tmp/config_metadata.json").write_text(json.dumps(meta, indent=2), encoding="utf-8")

    print(f"Generated {len(generated)} configuration files in {OUT_DIR}")
    for g in generated:
        print(f"  - {g}")


if __name__ == "__main__":
    main()
