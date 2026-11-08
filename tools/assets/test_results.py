"""Summary of a Unity test-results XML (NUnit 3): totals and each failed case with its message.

Usage: python -I tools/assets/test_results.py unity/Logs/em.xml [unity/Logs/pm.xml ...]
"""
import sys
import xml.etree.ElementTree as ET

for path in sys.argv[1:]:
    root = ET.parse(path).getroot()
    print(f"{path}: {root.get('result')} total {root.get('total')} passed {root.get('passed')} failed {root.get('failed')} skipped {root.get('skipped')}")
    for case in root.iter('test-case'):
        if case.get('result') == 'Failed':
            message = case.find('failure/message')
            text = (message.text or '').strip() if message is not None else ''
            print(f"  FAILED {case.get('fullname')}\n    {text[:1500]}")
