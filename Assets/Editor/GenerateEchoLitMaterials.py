#!/usr/bin/env python3
"""Generate URP Lit materials in Resources/EchoLit for Quest builds."""

import os
import re
import uuid

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
MINE_ROOT = os.path.join(ROOT, "Assets", "Mine", "Models")
LIT_DIR = os.path.join(ROOT, "Assets", "Resources", "EchoLit")
CATALOG_PATH = os.path.join(ROOT, "Assets", "Resources", "EchoMaterialCatalog.asset")
ECHO_SCAN_DIR = os.path.join(ROOT, "Assets", "Materials", "EchoScanGenerated")
LIT_SHADER_GUID = "933532a4fcc9baf4fa0491de14d08ed7"

NAMES = [
    "OreBlue", "Wood2", "Wood", "OreGreen", "Rock1", "Pickaxe",
    "OreRed", "Stone", "Hatch", "Rock2", "Crate", "OreStone",
]


def unity_guid():
    return uuid.uuid4().hex


def find_mine_mat(name):
    for dirpath, _, filenames in os.walk(MINE_ROOT):
        for filename in filenames:
            if filename == f"{name}.mat":
                return os.path.join(dirpath, filename)
    return None


def parse_mine_mat(path):
    text = open(path, encoding="utf-8").read()
    data = {
        "base_color": "1, 1, 1, 1",
        "base_map": None,
        "bump_map": None,
        "smoothness": "0.5",
        "metallic": "0",
        "emission": False,
        "emission_color": "0, 0, 0, 1",
        "emission_map": None,
        "lightmap_flags": "1",
    }

    color = re.search(r"_Color: \{r: ([^}]+)\}", text)
    if color:
        data["base_color"] = color.group(1)

    for prop, key in [("_MainTex", "base_map"), ("_BumpMap", "bump_map"), ("_EmissionMap", "emission_map")]:
        match = re.search(
            rf"- {re.escape(prop)}:\s*\n\s*m_Texture: \{{fileID: \d+, guid: ([a-f0-9]+), type: 3\}}",
            text,
        )
        if match:
            data[key] = match.group(1)

    gloss = re.search(r"_Glossiness: ([0-9.]+)", text)
    if gloss:
        data["smoothness"] = gloss.group(1)

    metallic = re.search(r"_Metallic: ([0-9.]+)", text)
    if metallic:
        data["metallic"] = metallic.group(1)

    if "_EMISSION" in text or "m_LightmapFlags: 6" in text:
        data["emission"] = True
        data["lightmap_flags"] = "6"
        em = re.search(r"_EmissionColor: \{r: ([^}]+)\}", text)
        if em:
            data["emission_color"] = em.group(1)

    flags = re.search(r"m_LightmapFlags: (\d+)", text)
    if flags:
        data["lightmap_flags"] = flags.group(1)

    return data


def echo_mat_guid(name):
    path = os.path.join(ECHO_SCAN_DIR, f"EchoScan_{name}.mat.meta")
    if not os.path.exists(path):
        return None
    return open(path, encoding="utf-8").read().split("guid: ")[1].split()[0]


def write_lit_mat(name, data, mat_guid):
    os.makedirs(LIT_DIR, exist_ok=True)
    keywords = []
    texenvs = []
    floats = [
        ("_Smoothness", data["smoothness"]),
        ("_Metallic", data["metallic"]),
    ]

    if data["base_map"]:
        texenvs.append(
            f"""    - _BaseMap:
        m_Texture: {{fileID: 2800000, guid: {data['base_map']}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}"""
        )

    if data["bump_map"]:
        keywords.append("  - _NORMALMAP")
        texenvs.append(
            f"""    - _BumpMap:
        m_Texture: {{fileID: 2800000, guid: {data['bump_map']}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}"""
        )

    if data["emission"]:
        keywords.append("  - _EMISSION")
        if data["emission_map"]:
            texenvs.append(
                f"""    - _EmissionMap:
        m_Texture: {{fileID: 2800000, guid: {data['emission_map']}, type: 3}}
        m_Scale: {{x: 1, y: 1}}
        m_Offset: {{x: 0, y: 0}}"""
            )

    keywords_block = "\n".join(keywords) if keywords else ""
    invalid_kw = "  m_InvalidKeywords: []" if keywords else "  m_InvalidKeywords: []"
    valid_kw = f"  m_ValidKeywords:\n{keywords_block}" if keywords else "  m_ValidKeywords: []"
    tex_block = "\n".join(texenvs) if texenvs else "    []"
    if texenvs:
        tex_block = "\n".join(texenvs)

    emission_color_line = ""
    if data["emission"]:
        emission_color_line = f"    - _EmissionColor: {{r: {data['emission_color']}}}\n"

    yaml = f"""%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!21 &2100000
Material:
  serializedVersion: 8
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: Lit_{name}
  m_Shader: {{fileID: 4800000, guid: {LIT_SHADER_GUID}, type: 3}}
  m_Parent: {{fileID: 0}}
  m_ModifiedSerializedProperties: 0
{valid_kw}
{invalid_kw}
  m_LightmapFlags: {data['lightmap_flags']}
  m_EnableInstancingVariants: 0
  m_DoubleSidedGI: 0
  m_CustomRenderQueue: -1
  stringTagMap: {{}}
  disabledShaderPasses: []
  m_LockedProperties: 
  m_SavedProperties:
    serializedVersion: 3
    m_TexEnvs:
{tex_block if tex_block else '    []'}
    m_Ints: []
    m_Floats:
    - _Smoothness: {data['smoothness']}
    - _Metallic: {data['metallic']}
    m_Colors:
    - _BaseColor: {{r: {data['base_color']}}}
{emission_color_line}  m_BuildTextureStacks: []
  m_AllowLocking: 1
"""

    mat_path = os.path.join(LIT_DIR, f"Lit_{name}.mat")
    open(mat_path, "w", encoding="utf-8").write(yaml)

    meta = f"""fileFormatVersion: 2
guid: {mat_guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: 2100000
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""
    open(mat_path + ".meta", "w", encoding="utf-8").write(meta)
    return mat_guid


def update_catalog(pairs):
    lines = open(CATALOG_PATH, encoding="utf-8").read().splitlines()
    out = []
    i = 0
    while i < len(lines):
        line = lines[i]
        if line.strip() == "entries:":
            out.append(line)
            for echo_guid, lit_guid in pairs:
                out.append("  - echoMaterial: {fileID: 2100000, guid: " + echo_guid + ", type: 2}")
                out.append("    originalMaterial: {fileID: 2100000, guid: " + lit_guid + ", type: 2}")
            while i + 1 < len(lines) and lines[i + 1].startswith("  - "):
                i += 1
            while i + 1 < len(lines) and lines[i + 1].strip().startswith("originalMaterial:"):
                i += 1
            i += 1
            continue
        out.append(line)
        i += 1

    open(CATALOG_PATH, "w", encoding="utf-8").write("\n".join(out) + "\n")


def main():
    pairs = []
    for name in NAMES:
        mine_path = find_mine_mat(name)
        if not mine_path:
            print(f"Missing mine material: {name}")
            continue
        echo_guid = echo_mat_guid(name)
        if not echo_guid:
            print(f"Missing echo material: {name}")
            continue
        data = parse_mine_mat(mine_path)
        lit_guid = unity_guid()
        write_lit_mat(name, data, lit_guid)
        pairs.append((echo_guid, lit_guid))
        print(f"Lit_{name} -> {lit_guid}")

    update_catalog(pairs)
    print(f"Updated catalog with {len(pairs)} entries.")


if __name__ == "__main__":
    main()
