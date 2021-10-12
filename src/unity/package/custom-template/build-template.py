import json
from os import system
from os.path import exists
from json import dumps
from argparse import ArgumentParser

parser = ArgumentParser(add_help=True)
parser.add_argument('-n', '--name', required=True, help='Name')
parser.add_argument('-d', '--description', dest='descr', required=True, help='Description')
parser.add_argument('-v', '--package-version', dest='pckg_version', default='0.1.0', help='Package Version')
parser.add_argument('-u', '--unity-version', dest='unity_version', default='2020.1', help='Unity Version')
parser.add_argument('-z', '--7zip', dest='zip_path', required=True, help='7zip executable')
parser.add_argument('-t', '--template-folder', dest='tmplt_path', required=True, help='Template Folder')
parser.add_argument('-p', '--package-folder', dest='pckg_path', required=True, help='Package Folder')
parser.add_argument('-g', '--target-path', dest='tgt_path', required=True, help='Target Folder')

args = parser.parse_args()

pckg_json = {
    'name': f'''com.unity.template.{
        (str)(args.name).lower()
    }''',
    'description': args.descr,
    'displayName': args.name,
    'version': args.pckg_version,
    'type': 'template',
    'host': 'hub',
    'unity': args.unity_version
}

with open(f'package.json', 'w') as file:
    file.write(
        json.dumps(pckg_json)
    )

# del TGT_PATH
# %SEVENZ% a -ttar %TGT_PATH% %PCKG_FOLDER%