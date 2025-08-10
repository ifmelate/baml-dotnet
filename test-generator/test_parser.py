#!/usr/bin/env python3

import re

# Test the regex patterns from the C# parser
class_regex = re.compile(r'class\s+(\w+)\s*\{([^}]*)\}', re.MULTILINE | re.DOTALL)
property_regex = re.compile(r'^\s*(\w+)\s+(\w+)(?:\s*@description\(([^)]+)\))?\s*$', re.MULTILINE)

content = """class Person {
    name string
    age int
}

function GetPerson(id: string) -> Person {
    client "test/model"
    prompt #"Get person with id {{ id }}"#
}"""

print("Testing BAML parsing...")
print("Content:")
print(content)
print("\n" + "="*50 + "\n")

# Test class parsing
class_matches = class_regex.findall(content)
print(f"Found {len(class_matches)} classes:")
for class_name, class_body in class_matches:
    print(f"  Class: {class_name}")
    print(f"  Body: '{class_body}'")
    
    # Test property parsing
    lines = class_body.split('\n')
    print(f"  Lines in body: {lines}")
    
    for line in lines:
        trimmed = line.strip()
        if not trimmed:
            continue
        print(f"    Testing line: '{trimmed}'")
        prop_match = property_regex.match(trimmed)
        if prop_match:
            print(f"      Property: {prop_match.groups()}")
        else:
            print(f"      No match for: '{trimmed}'")
    print()

