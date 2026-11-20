from pathlib import Path
import json,hashlib,zipfile,dnfile,re
from dncil.cil.body.reader import read_method_body_from_bytes
root=Path(__file__).resolve().parent.parent
build=root/'t3485_quad30'
state=json.loads((build/'validation_0.1.0.json').read_text(encoding='utf-8-sig'))
validation=Path(state['directory'])
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
actual=root/'T3485Quad30.dll';tested=build/'T3485Quad30.dll';assert actual.exists() and tested.exists() and sha(actual)==sha(tested),'Installed DLL differs from compiled quad build'
report=(root/'t3485q30-preflight.txt').read_text(encoding='utf-8');log=(validation/'MelonLoader_final_preflight.log').read_text(encoding='utf-8')
assert report.startswith('PASS native quad-30mm preflight:'),'Missing final quad native preflight'
for text in [
 'removed 85mm and old coax','native BMP2 2A42 mesh x4','native four-barrel left-to-right MultiBarrels','each barrel 850rpm, aggregate 3400rpm','27 deg/s horizontal and 31 deg/s vertical',
 'PASS native Shift entry after optic hiding: quad 30mm -> Leopard day, ATGM -> launcher','PASS quad mounting: old85mm mantle/barrel/breech faces removed','cropped original T34 paint mounting plate','PASS shield paint: original visible T34 exterior material','T34_co 64x64 pixels from (80,384)','PASS shield GPU pixels:','PASS T-34 armor: front/side structural hull and all turret armor +50mm','PASS Fulda Kontakt-1: 103 independently paired ERA tiles','Quad 30mm: 4 barrels; 850rpm each; aggregate cycle 0.01764706s; left-to-right sequence; 3UBR11 600 rounds 163mm/1210m/s/0kg; 3UOF21 1200 rounds 10mm/920m/s/0.049kg; original 30mm fragment fields inherited.'
]: assert text in report,'Missing quad verification: '+text
assert '[T3485Q30MOD] preparation failed' not in log,'Quad preparation failed in final loader log'
assert state.get('dll_sha256')==sha(actual),'Preflight state does not identify the installed DLL'
assert sha(actual) in log.upper(),'Final loader log loaded a different DLL'
assert (validation/'preflight_final.txt').read_bytes()==(root/'t3485q30-preflight.txt').read_bytes(),'Archived report differs'
assert (validation/'renders/shield_source_patch.png').read_bytes()==(validation/'renders/shield_cropped_patch.png').read_bytes(),'GPU albedo crop differs from original paint pixels'
assembly=dnfile.dnPE(str(actual));refs=[str(x.Name) for x in assembly.net.mdtables.AssemblyRef]
assert not any(x in refs for x in ['UnderdogsEnhanced','PactIncreasedLethality','Leopard2A7V','Leopard2A7HU','Mono.Cecil','dnlib']),'Unexpected dependency'
count=0
for m in assembly.net.mdtables.MethodDef:
 if m.Rva:read_method_body_from_bytes(assembly.get_data(m.Rva,100000));count+=1
payload=[r for r in assembly.net.resources if r.name=='T3485Quad30.2a42.meshbin'];assert len(payload)==1 and bytes(payload[0].data)==(build/'2a42.meshbin').read_bytes(),'Embedded model differs'
zip_path=build/'dist/T3485Quad30-0.1.0.zip';assert zip_path.exists()
with zipfile.ZipFile(zip_path) as z:
 assert z.testzip() is None,'Invalid archive';names=z.namelist();assert [x for x in names if x.lower().endswith('.dll')]==['Mods/T3485Quad30.dll'],'Wrong packaged DLL list';assert z.read('Mods/T3485Quad30.dll')==actual.read_bytes(),'Packaged DLL differs'
 for source in ['QuadGeometry.cs','ShieldPaint.cs','QuadWeaponConversion.cs','QuadMod.cs','NativeValidation.cs','ManagedTests.cs','README.md']:
  assert 'Source/t3485_quad30/'+source in names,'Missing quad source '+source
  assert z.read('Source/t3485_quad30/'+source)==(build/source).read_bytes(),'Packaged source differs: '+source
 assert 'Validation/native_preflight.txt' in names and z.read('Validation/native_preflight.txt')==(root/'t3485q30-preflight.txt').read_bytes(),'Packaged validation differs'
 for image in ['front_three_quarter.png','era_front.png','era_left.png','era_right.png','era_roof.png','shield_source_patch.png','shield_cropped_patch.png']:
  assert 'Validation/renders/'+image in names,'Missing render '+image
  assert z.read('Validation/renders/'+image)==(validation/'renders'/image).read_bytes(),'Stale packaged render: '+image
 readme=z.read('Mods/T3485Quad30/README_zh.md').decode('utf-8-sig');assert 'T3485Q30MOD' in readme and '3UBR11' in readme and '3UOF21' in readme,'Obsolete quad readme'
base_root=root/'t3485_modernized';baseline=Path(json.loads((base_root/'runtime_check.json').read_text(encoding='utf-8-sig'))['directory'])
original_mods=json.loads((baseline/'original_mod_hashes.json').read_text(encoding='utf-8-sig'))
known_external_mod_hashes={'GHPCCombatOverhaul.dll':'299EABDC8FA0D652812174FF77D21674CC59C84534B2BB3FB1CA71573181CC25'}
mod_mismatches=[]
for item in original_mods:
 actual_hash=sha(root/item['file'])
 if actual_hash!=item['sha256']:
  if item['file'] not in known_external_mod_hashes or actual_hash!=known_external_mod_hashes[item['file']]: raise AssertionError(item['file'])
  mod_mismatches.append(item['file'])
for item in json.loads((baseline/'original_game_hashes.json').read_text(encoding='utf-8-sig')):assert sha(Path(item['Path']))==item['Hash'],item['Path']
manifest=dict(version='0.1.0',vehicle_key='T3485Q30MOD',display_name='T-34-85M Quad 30mm',date='2026-11-19',shield_material='original visible T34 exterior shader with five cropped native maps; no color override',shield_crop_source_pixels=[80,384,64,64],installed_dll=str(actual),dll_sha256=sha(actual),package=str(zip_path),package_sha256=sha(zip_path),managed_methods_parsed=count,dependencies=refs,embedded_2a42_sha256=sha(build/'2a42.meshbin'),original_mod_dlls_verified=len(original_mods)-len(mod_mismatches),original_mod_hash_mismatches=mod_mismatches,native_preflight_log=str(validation/'preflight_final.txt'),weapon='4x BMP2 2A42 native MultiBarrels',barrel_rpm=850,aggregate_rpm=3400,sequence='left-to-right',horizontal_traverse_deg_per_s=27,vertical_traverse_deg_per_s=31,ap_name='3UBR11',ap_rounds=600,ap_pen_mm=163,ap_velocity_mps=1210,ap_tnt_kg=0,he_name='3UOF21',he_rounds=1200,he_pen_mm=10,he_velocity_mps=920,he_tnt_kg=0.049,fragment_source='native original 30mm AP and HE fields',armor_bonus_mm=50,era_tile_count=103,era_regions=dict((a,int(b)) for a,b in re.findall(r'(HullFront|HullLeft|HullRight|TurretFront|TurretLeft|TurretRight|TurretRoof)=(\d+)',report)),validation_limit='Native four-barrel MultiBarrels and independent registration; 240 sampled ERA motion poses; no active mission firing or drive test')
for target in [build/'install_manifest.json',root/'T3485Quad30/install_manifest.json']:
 target.parent.mkdir(parents=True,exist_ok=True);target.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('PASS quad 30mm native preflight, mounting/removal, fragmentation inheritance, package and original-file verification')
if mod_mismatches: print('NOTE pre-existing external mod hash mismatch:', ', '.join(mod_mismatches))
print('DLL SHA256',manifest['dll_sha256']);print('ZIP SHA256',manifest['package_sha256']);print('Package bytes',zip_path.stat().st_size)
