import UnityPy,json
from pathlib import Path
E=UnityPy.load('../GHPC_Data/StreamingAssets/aa/StandaloneWindows64/vehicles_assets_all_13aeb3cd2c4b75ffc0ce491c7eb581a5.bundle')
M={o.path_id:o for o in E.objects}
S={o.path_id:o.read().m_Name for o in E.objects if o.type.name=='MonoScript'}
rows=[]
def walk(pid,path=''):
 g=M[pid].read();path+='/'+g.m_Name
 comps=[M[q.m_PathID] for q in g.m_Components]
 scripts=[]
 for c in comps:
  if c.type.name=='MonoBehaviour':
   d=c.read_typetree();scripts.append(S.get(d.get('m_Script',{}).get('m_PathID'),'?'))
 for c in comps:
  if c.type.name not in ['MeshRenderer','SkinnedMeshRenderer']:continue
  d=c.read();mats=[]
  for mat in d.m_Materials:
   m=M.get(mat.m_PathID)
   if not m:mats.append('external');continue
   md=m.read();sh=M.get(md.m_Shader.m_PathID)
   mats.append(dict(name=md.m_Name,shader=(sh.read_typetree().get('m_Name','?') if sh else 'external')))
  rows.append(dict(path=path,layer=g.m_Layer,active=g.m_IsActive,enabled=d.m_Enabled,scripts=scripts,materials=mats))
 t=next(c.read() for c in comps if c.type.name=='Transform')
 for child in t.m_Children:walk(M[child.m_PathID].read().m_GameObject.m_PathID,path)
walk(2951283845703832543)
Path('t3485_quad30/model_inventory.json').write_text(json.dumps(rows,indent=2),encoding='utf8')
for row in rows:
 if '/T34_rig/' in row['path'] or '/T34_Turret_armour/' in row['path'] or '/T-34-85 GUN FOLLOW/' in row['path'] or row['scripts']==['Compartment']:print(json.dumps(row))
