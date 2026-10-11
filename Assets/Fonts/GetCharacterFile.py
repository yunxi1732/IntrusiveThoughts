#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
读取 Assets/Resources/CSV 下所有文件内容，
去重所有字符，输出到 Assets/Fonts/characterfile.txt（单行，无换行）。
"""

from pathlib import Path

# 脚本位于 Assets/Fonts 下，项目根为脚本的上两级
script_dir = Path(__file__).resolve().parent
project_root = script_dir.parent.parent

csv_dir = project_root / "Assets" / "Resources" / "CSV"
output = project_root / "Assets" / "Fonts" / "characterfile.txt"

# 收集字符（set 自动去重）
chars = set()
for path in csv_dir.rglob("*"):
    if not path.is_file() or path.suffix.lower() == ".meta":
        continue
    try:
        text = path.read_text(encoding="utf-8-sig", errors="ignore")
    except Exception as e:
        print(f"[WARN] 读取失败，已跳过: {path} ({e})")
        continue
    chars.update(text)

# 排除换行符本身，排序后拼成一行
chars.discard("\r")
chars.discard("\n")
content = "".join(sorted(chars))

output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(content, encoding="utf-8")

print(f"[INFO] 已写入: {output}")
print(f"[INFO] 去重后字符数: {len(chars)}")