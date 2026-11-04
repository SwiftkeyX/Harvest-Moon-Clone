"""Read and edit Unity Shader Graph files (.shadergraph, Shader Graph 17.3).

A .shadergraph is "multi-JSON": many JSON objects one after another. Object 0 is the GraphData; every node,
slot, property, keyword and group is its own object, linked by m_ObjectId. Wires live in GraphData.m_Edges and
point at (node m_ObjectId, slot m_Id).

CLI:
    python -I sg_graph.py <file.shadergraph> list

Library (from a small script next to it):
    import sys; sys.path.insert(0, r"<skill>/scripts"); from sg_graph import Graph
    g = Graph.load(path)
    noise = g.custom_function("CliffStreakNoise", source_guid, inputs=[("PositionWS", "v3"), ...],
                              outputs=[("Noise", "f")], x=-800, y=-100, group=g.group("Streaks", -1060, -200))
    p = g.property("Line Width", "f", 0.085, slider=(0, 0.2))
    g.wire(g.property_node(p, x=-560, y=280), 0, noise, "Width")
    g.save()

Always save and close the graph window in Unity before editing (see SKILL.md), and back the file up.
"""
import copy
import json
import sys
import uuid

SG = 'UnityEditor.ShaderGraph.'
SLOT_TYPES = {'f': 'Vector1MaterialSlot', 'v2': 'Vector2MaterialSlot', 'v3': 'Vector3MaterialSlot',
              'v4': 'Vector4MaterialSlot', 'b': 'BooleanMaterialSlot'}
ZERO = {'f': 0.0, 'b': False, 'v2': {'x': 0.0, 'y': 0.0}, 'v3': {'x': 0.0, 'y': 0.0, 'z': 0.0},
        'v4': {'x': 0.0, 'y': 0.0, 'z': 0.0, 'w': 0.0}}
# Output slot type of a PropertyNode for each property kind.
PROP_SLOT = {'f': 'f', 'b': 'b', 'v2': 'v2', 'v3': 'v3', 'color': 'v4'}


def _id():
    return uuid.uuid4().hex


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_rgb(h):
    h = h.lstrip('#')
    return [int(h[k:k + 2], 16) / 255 for k in (0, 2, 4)]


class Graph:
    def __init__(self, path, objs):
        self.path, self.objs = path, objs
        self.by = {o['m_ObjectId']: o for o in objs}
        self.g = objs[0]

    # ---------- load / save ----------
    @classmethod
    def load(cls, path):
        txt = open(path, encoding='utf-8').read()
        dec = json.JSONDecoder(); i = 0; objs = []
        while i < len(txt):
            while i < len(txt) and txt[i].isspace():
                i += 1
            if i >= len(txt):
                break
            o, j = dec.raw_decode(txt, i); objs.append(o); i = j
        return cls(path, objs)

    def save(self, path=None):
        out = '\n\n'.join(json.dumps(o, indent=4) for o in self.objs) + '\n'
        open(path or self.path, 'w', encoding='utf-8', newline='\n').write(out)

    def _add(self, o):
        self.objs.append(o); self.by[o['m_ObjectId']] = o
        return o

    # ---------- lookup ----------
    def nodes(self):
        return [self.by[n['m_Id']] for n in self.g['m_Nodes']]

    def slots(self, node):
        return [self.by[s['m_Id']] for s in node['m_Slots']]

    def slot(self, node, name):
        for s in self.slots(node):
            if s['m_DisplayName'] == name or s['m_ShaderOutputName'] == name:
                return s
        raise KeyError(f"{node.get('m_Name')} has no slot {name}")

    def properties(self):
        return {self.by[p['m_Id']]['m_Name']: self.by[p['m_Id']] for p in self.g['m_Properties']}

    def custom(self, function_name):
        for o in self.nodes():
            if o['m_Type'].endswith('CustomFunctionNode') and o['m_FunctionName'] == function_name:
                return o
        raise KeyError(function_name)

    def block(self, descriptor='SurfaceDescription.BaseColor'):
        for o in self.nodes():
            if o.get('m_SerializedDescriptor') == descriptor:
                return o
        raise KeyError(descriptor)

    def node_of_type(self, type_suffix):
        return next(o for o in self.nodes() if o['m_Type'].endswith(type_suffix))

    def property_nodes(self, prop):
        return [o for o in self.nodes() if o['m_Type'].endswith('PropertyNode') and o['m_Property']['m_Id'] == prop['m_ObjectId']]

    def any_custom_source(self):
        """GUID of the .hlsl file an existing Custom Function node uses (reuse it for new nodes)."""
        return next(o['m_FunctionSource'] for o in self.nodes() if o['m_Type'].endswith('CustomFunctionNode'))

    # ---------- building blocks ----------
    def _slot(self, kind, sid, name, out, value=None):
        o = {'m_SGVersion': 0, 'm_Type': SG + SLOT_TYPES[kind], 'm_ObjectId': _id(), 'm_Id': sid,
             'm_DisplayName': name, 'm_SlotType': 1 if out else 0, 'm_Hidden': False,
             'm_ShaderOutputName': name, 'm_StageCapability': 3}
        if kind == 'b':
            o.update({'m_Value': bool(value), 'm_DefaultValue': False})
        else:
            o.update({'m_Value': copy.deepcopy(value if value is not None else ZERO[kind]),
                      'm_DefaultValue': copy.deepcopy(ZERO[kind]), 'm_Labels': []})
            if kind == 'f':
                o['m_LiteralMode'] = False
        return self._add(o)

    def _node(self, type_name, name, x, y, slot_objs, extra, group=None, sg_version=0, w=200.0, h=150.0):
        o = {'m_SGVersion': sg_version, 'm_Type': SG + type_name, 'm_ObjectId': _id(),
             'm_Group': {'m_Id': group or ''}, 'm_Name': name,
             'm_DrawState': {'m_Expanded': True, 'm_Position': {'serializedVersion': '2', 'x': float(x), 'y': float(y),
                                                               'width': w, 'height': h}},
             'm_Slots': [{'m_Id': s['m_ObjectId']} for s in slot_objs], 'synonyms': [], 'm_Precision': 0,
             'm_PreviewExpanded': True, 'm_DismissedVersion': 0, 'm_PreviewMode': 0,
             'm_CustomColors': {'m_SerializableColors': []}}
        o.update(extra)
        self._add(o)
        self.g['m_Nodes'].append({'m_Id': o['m_ObjectId']})
        return o

    def custom_function(self, function_name, source_guid, inputs, outputs, x=0, y=0, group=None):
        """Custom Function node in File mode. inputs/outputs: [(name, kind) or (name, kind, value)], in the
        same order as the HLSL parameters. function_name is the HLSL name without the _float suffix."""
        spec = [(i, False) for i in inputs] + [(o, True) for o in outputs]
        slots = [self._slot(s[1], n, s[0], out, s[2] if len(s) > 2 else None) for n, (s, out) in enumerate(spec)]
        return self._node('CustomFunctionNode', function_name + ' (Custom Function)', x, y, slots,
                          {'synonyms': ['code', 'HLSL'], 'm_SourceType': 0, 'm_FunctionName': function_name,
                           'm_FunctionSource': source_guid, 'm_FunctionSourceUsePragmas': True,
                           'm_FunctionBody': 'Enter function body here...'}, group, sg_version=1, w=260.0, h=220.0)

    def position_node(self, x=0, y=0, group=None, space=2):
        """space: 0 Object, 1 View, 2 World, 3 Tangent, 4 Absolute World."""
        out = self._slot('v3', 0, 'Out', True)
        return self._node('PositionNode', 'Position', x, y, [out],
                          {'synonyms': ['location'], 'm_Precision': 1, 'm_PreviewMode': 2, 'm_Space': space,
                           'm_PositionSource': 0}, group, sg_version=1, w=208.0, h=312.0)

    def normal_node(self, x=0, y=0, group=None, space=2):
        out = self._slot('v3', 0, 'Out', True)
        return self._node('NormalVectorNode', 'Normal Vector', x, y, [out],
                          {'synonyms': ['surface direction'], 'm_Precision': 1, 'm_PreviewMode': 2, 'm_Space': space},
                          group, w=208.0, h=312.0)

    def property(self, name, kind, default, slider=None, reference=None):
        """Blackboard property. kind: 'f' float, 'b' boolean, 'v2'/'v3' vectors, 'color' (default '#rrggbb').
        The default only seeds NEW materials and graph previews; existing materials keep their own values."""
        if name in self.properties():
            return self.properties()[name]
        ref = reference or '_' + name.replace(' ', '_')
        p = {'m_SGVersion': 1, 'm_ObjectId': _id(), 'm_Guid': {'m_GuidSerialized': str(uuid.uuid4())},
             'promotedFromAssetID': '', 'promotedFromCategoryName': '', 'promotedOrdering': -1,
             'm_Name': name, 'm_DefaultRefNameVersion': 1, 'm_RefNameGeneratedByDisplayName': name,
             'm_DefaultReferenceName': ref, 'm_OverrideReferenceName': '', 'm_GeneratePropertyBlock': True,
             'm_UseCustomSlotLabel': False, 'm_CustomSlotLabel': '', 'm_DismissedVersion': 0, 'm_Precision': 0,
             'overrideHLSLDeclaration': False, 'hlslDeclarationOverride': 0, 'm_Hidden': False,
             'm_PerRendererData': False, 'm_customAttributes': []}
        if kind == 'f':
            p.update({'m_Type': SG + 'Internal.Vector1ShaderProperty', 'm_Value': float(default),
                      'm_FloatType': 1 if slider else 0, 'm_LiteralFloatMode': False,
                      'm_RangeValues': {'x': float(slider[0]), 'y': float(slider[1])} if slider else {'x': 0.0, 'y': 1.0},
                      'm_SliderType': 0, 'm_SliderPower': 3.0, 'm_EnumType': 0, 'm_CSharpEnumString': '',
                      'm_EnumNames': ['Default'], 'm_EnumValues': [0]})
        elif kind == 'b':
            p.update({'m_Type': SG + 'Internal.BooleanShaderProperty', 'm_Value': bool(default)})
        elif kind == 'v2':
            p.update({'m_Type': SG + 'Internal.Vector2ShaderProperty',
                      'm_Value': {'x': default[0], 'y': default[1], 'z': 0.0, 'w': 0.0}})
        elif kind == 'v3':
            p.update({'m_Type': SG + 'Internal.Vector3ShaderProperty',
                      'm_Value': {'x': default[0], 'y': default[1], 'z': default[2], 'w': 0.0}})
        elif kind == 'color':
            r, g, b = hex_rgb(default)  # stored as picked (sRGB)
            p.update({'m_SGVersion': 3, 'm_Type': SG + 'Internal.ColorShaderProperty',
                      'm_Value': {'r': r, 'g': g, 'b': b, 'a': 1.0}, 'isMainColor': False, 'm_ColorMode': 0})
        else:
            raise ValueError(kind)
        self._add(p)
        self.g['m_Properties'].append({'m_Id': p['m_ObjectId']})
        self._category()['m_ChildObjectList'].append({'m_Id': p['m_ObjectId']})
        return p

    def property_node(self, prop, x=0, y=0, group=None):
        kind = {'Vector1ShaderProperty': 'f', 'BooleanShaderProperty': 'b', 'Vector2ShaderProperty': 'v2',
                'Vector3ShaderProperty': 'v3',
                'ColorShaderProperty': 'color'}[prop['m_Type'].split('.')[-1]]
        out = self._slot(PROP_SLOT[kind], 0, prop['m_Name'], True)
        out['m_ShaderOutputName'] = 'Out'
        return self._node('PropertyNode', 'Property', x, y, [out], {'m_Property': {'m_Id': prop['m_ObjectId']}},
                          group, w=170.0, h=34.0)

    def keyword(self, name, reference, multi_compile=True, global_scope=True, exposed=False):
        """Boolean keyword, e.g. the URP shadow keywords an Unlit graph needs for GetMainLight shadows."""
        for k in self.g['m_Keywords']:
            o = self.by[k['m_Id']]
            if (o.get('m_OverrideReferenceName') or o.get('m_DefaultReferenceName')) == reference:
                return o
        k = self._add({'m_SGVersion': 1, 'm_Type': SG + 'ShaderKeyword', 'm_ObjectId': _id(),
                       'm_Guid': {'m_GuidSerialized': str(uuid.uuid4())}, 'promotedFromAssetID': '',
                       'promotedFromCategoryName': '', 'promotedOrdering': -1, 'm_Name': name,
                       'm_DefaultRefNameVersion': 1, 'm_RefNameGeneratedByDisplayName': name,
                       'm_DefaultReferenceName': reference, 'm_OverrideReferenceName': reference,
                       'm_GeneratePropertyBlock': exposed, 'm_UseCustomSlotLabel': False, 'm_CustomSlotLabel': '',
                       'm_DismissedVersion': 0, 'm_KeywordType': 0, 'm_KeywordDefinition': 1 if multi_compile else 0,
                       'm_KeywordScope': 1 if global_scope else 0, 'm_KeywordStages': 63, 'm_Entries': [],
                       'm_Value': 0, 'm_IsEditable': True})
        self.g['m_Keywords'].append({'m_Id': k['m_ObjectId']})
        self._category()['m_ChildObjectList'].append({'m_Id': k['m_ObjectId']})
        return k

    def shadow_keywords(self):
        for name, ref in [('Main Light Shadows', '_MAIN_LIGHT_SHADOWS'),
                          ('Main Light Shadows Cascade', '_MAIN_LIGHT_SHADOWS_CASCADE'),
                          ('Soft Shadows', '_SHADOWS_SOFT')]:
            self.keyword(name, ref)

    def group(self, title, x, y):
        """Labelled group box. Returns its id; pass it as group= when creating or placing nodes."""
        for gd in self.g['m_GroupDatas']:
            o = self.by[gd['m_Id']]
            if o['m_Title'] == title:
                return o['m_ObjectId']
        o = self._add({'m_SGVersion': 0, 'm_Type': SG + 'GroupData', 'm_ObjectId': _id(), 'm_Title': title,
                       'm_Position': {'x': float(x), 'y': float(y)}})
        self.g['m_GroupDatas'].append({'m_Id': o['m_ObjectId']})
        return o['m_ObjectId']

    def place(self, node, x, y, group=None):
        node['m_DrawState']['m_Position'].update({'x': float(x), 'y': float(y)})
        if group is not None:
            node['m_Group'] = {'m_Id': group}

    def wire(self, src_node, src_slot, dst_node, dst_slot):
        """Connect an output to an input. Slots can be names or m_Id numbers. Replaces any wire already on the input."""
        si = src_slot if isinstance(src_slot, int) else self.slot(src_node, src_slot)['m_Id']
        di = dst_slot if isinstance(dst_slot, int) else self.slot(dst_node, dst_slot)['m_Id']
        self.g['m_Edges'] = [e for e in self.g['m_Edges'] if not (
            e['m_InputSlot']['m_Node']['m_Id'] == dst_node['m_ObjectId'] and e['m_InputSlot']['m_SlotId'] == di)]
        self.g['m_Edges'].append({'m_OutputSlot': {'m_Node': {'m_Id': src_node['m_ObjectId']}, 'm_SlotId': si},
                                  'm_InputSlot': {'m_Node': {'m_Id': dst_node['m_ObjectId']}, 'm_SlotId': di}})

    def set_input(self, node, slot_name, value):
        """Value typed into an unwired input. Vector3 colours must be linear: use srgb_to_linear."""
        self.slot(node, slot_name)['m_Value'] = value

    def _category(self):
        return next(o for o in self.objs if o['m_Type'].endswith('CategoryData'))

    # ---------- new graphs from an existing one ----------
    def strip(self, keep_keywords=True):
        """Remove every node except the output blocks, plus all wires, properties and groups, so this graph can
        be saved under a new name as an empty graph with the same target. Block inputs are unwired."""
        keep = {n['m_Id'] for n in self.g['m_Nodes'] if self.by[n['m_Id']]['m_Type'].endswith('BlockNode')}
        self.g['m_Nodes'] = [n for n in self.g['m_Nodes'] if n['m_Id'] in keep]
        self.g['m_Edges'] = []
        self.g['m_Properties'] = []
        self.g['m_GroupDatas'] = []
        if not keep_keywords:
            self.g['m_Keywords'] = []
        live = {k['m_Id'] for k in self.g['m_Keywords']}
        self._category()['m_ChildObjectList'] = [c for c in self._category()['m_ChildObjectList'] if c['m_Id'] in live]
        self._prune()

    def _prune(self):
        """Drop objects nothing points at any more (old nodes, slots, properties)."""
        txt_refs = set()

        def walk(v):
            if isinstance(v, dict):
                if set(v.keys()) == {'m_Id'} and isinstance(v['m_Id'], str):
                    txt_refs.add(v['m_Id'])
                for x in v.values():
                    walk(x)
            elif isinstance(v, list):
                for x in v:
                    walk(x)
        keep = {self.g['m_ObjectId']}
        frontier = [self.g]
        while frontier:
            txt_refs.clear()
            for o in frontier:
                walk(o)
            frontier = [self.by[r] for r in txt_refs if r in self.by and r not in keep]
            keep.update(o['m_ObjectId'] for o in frontier)
        self.objs = [o for o in self.objs if o['m_ObjectId'] in keep]
        self.by = {o['m_ObjectId']: o for o in self.objs}

    def target(self):
        return next(o for o in self.objs if o['m_Type'].endswith('UniversalTarget'))

    def set_target(self, render_face=None, cast_shadows=None, alpha_clip=None, sub_target=None):
        """render_face: 'front' | 'back' | 'both'. sub_target: 'unlit' | 'sprite_unlit' (Sprite Unlit has no fog)."""
        t = self.target()
        if render_face:
            t['m_RenderFace'] = {'front': 2, 'back': 1, 'both': 0}[render_face]
        if cast_shadows is not None:
            t['m_CastShadows'] = bool(cast_shadows)
        if alpha_clip is not None:
            t['m_AlphaClip'] = bool(alpha_clip)
        if sub_target:
            st = self.by[t['m_ActiveSubTarget']['m_Id']]
            st['m_Type'] = 'UnityEditor.Rendering.Universal.ShaderGraph.' + {
                'unlit': 'UniversalUnlitSubTarget', 'sprite_unlit': 'UniversalSpriteUnlitSubTarget'}[sub_target]
            for k in list(st.keys()):
                if k not in ('m_SGVersion', 'm_Type', 'm_ObjectId'):
                    del st[k]

    def add_block(self, descriptor, kind='f', value=1.0, stage=2):
        """Output block, e.g. 'SurfaceDescription.Alpha'. Fragment blocks use stage 2, vertex blocks stage 1."""
        try:
            return self.block(descriptor)
        except KeyError:
            pass
        name = descriptor.split('.')[-1]
        s = self._slot(kind, 0, name, False, value)
        s['m_StageCapability'] = stage
        o = self._node('BlockNode', descriptor, 0, 0, [s], {'m_SerializedDescriptor': descriptor}, w=0.0, h=0.0)
        self.g['m_Nodes'].remove({'m_Id': o['m_ObjectId']})
        self.g['m_Nodes'].append({'m_Id': o['m_ObjectId']})
        ctx = self.g['m_FragmentContext'] if descriptor.startswith('SurfaceDescription') else self.g['m_VertexContext']
        ctx['m_Blocks'].append({'m_Id': o['m_ObjectId']})
        return o

    def texture_property(self, name, texture_guid=''):
        if name in self.properties():
            return self.properties()[name]
        p = {'m_SGVersion': 0, 'm_Type': SG + 'Internal.Texture2DShaderProperty', 'm_ObjectId': _id(),
             'm_Guid': {'m_GuidSerialized': str(uuid.uuid4())}, 'promotedFromAssetID': '', 'promotedFromCategoryName': '',
             'promotedOrdering': -1, 'm_Name': name, 'm_DefaultRefNameVersion': 1,
             'm_RefNameGeneratedByDisplayName': name, 'm_DefaultReferenceName': '_' + name.replace(' ', '_'),
             'm_OverrideReferenceName': '', 'm_GeneratePropertyBlock': True, 'm_UseCustomSlotLabel': False,
             'm_CustomSlotLabel': '', 'm_DismissedVersion': 0, 'm_Precision': 0, 'overrideHLSLDeclaration': False,
             'hlslDeclarationOverride': 0, 'm_Hidden': False, 'm_PerRendererData': False, 'm_customAttributes': [],
             'm_Value': {'m_SerializedTexture': '', 'm_Guid': texture_guid}, 'isMainTexture': False,
             'useTilingAndOffset': False, 'useTexelSize': True, 'isHDR': False, 'm_Modifiable': True, 'm_DefaultType': 0}
        self._add(p)
        self.g['m_Properties'].append({'m_Id': p['m_ObjectId']})
        self._category()['m_ChildObjectList'].append({'m_Id': p['m_ObjectId']})
        return p

    def texture_property_node(self, prop, x=0, y=0, group=None):
        out = self._add({'m_SGVersion': 0, 'm_Type': SG + 'Texture2DMaterialSlot', 'm_ObjectId': _id(), 'm_Id': 0,
                         'm_DisplayName': prop['m_Name'], 'm_SlotType': 1, 'm_Hidden': False,
                         'm_ShaderOutputName': 'Out', 'm_StageCapability': 3, 'm_BareResource': False})
        return self._node('PropertyNode', 'Property', x, y, [out], {'m_Property': {'m_Id': prop['m_ObjectId']}},
                          group, w=200.0, h=34.0)

    def uv_node(self, x=0, y=0, group=None, channel=0):
        out = self._slot('v4', 0, 'Out', True)
        return self._node('UVNode', 'UV', x, y, [out], {'synonyms': ['texcoords', 'coords', 'coordinates'],
                                                         'm_OutputChannel': channel}, group, w=208.0, h=312.0)

    def sample_texture_node(self, x=0, y=0, group=None):
        """Slots: RGBA 0, R 4, G 5, B 6, A 7 (outputs); Texture 1, UV 2, Sampler 3 (inputs)."""
        def s(t, sid, name, out, extra):
            o = {'m_SGVersion': 0, 'm_Type': SG + t, 'm_ObjectId': _id(), 'm_Id': sid, 'm_DisplayName': name,
                 'm_SlotType': 1 if out else 0, 'm_Hidden': False, 'm_ShaderOutputName': name,
                 'm_StageCapability': 2 if out else 3}
            o.update(extra)
            return self._add(o)
        v4z = {'x': 0.0, 'y': 0.0, 'z': 0.0, 'w': 0.0}
        f = {'m_Value': 0.0, 'm_DefaultValue': 0.0, 'm_Labels': [], 'm_LiteralMode': False}
        slots = [s('Vector4MaterialSlot', 0, 'RGBA', True, {'m_Value': dict(v4z), 'm_DefaultValue': dict(v4z), 'm_Labels': []}),
                 s('Vector1MaterialSlot', 4, 'R', True, dict(f)), s('Vector1MaterialSlot', 5, 'G', True, dict(f)),
                 s('Vector1MaterialSlot', 6, 'B', True, dict(f)), s('Vector1MaterialSlot', 7, 'A', True, dict(f)),
                 s('Texture2DInputMaterialSlot', 1, 'Texture', False,
                   {'m_BareResource': False, 'm_Texture': {'m_SerializedTexture': '', 'm_Guid': ''}, 'm_DefaultType': 0}),
                 s('UVMaterialSlot', 2, 'UV', False, {'m_Value': {'x': 0.0, 'y': 0.0}, 'm_DefaultValue': {'x': 0.0, 'y': 0.0},
                                                      'm_Labels': [], 'm_Channel': 0}),
                 s('SamplerStateMaterialSlot', 3, 'Sampler', False, {'m_BareResource': False})]
        return self._node('SampleTexture2DNode', 'Sample Texture 2D', x, y, slots,
                          {'synonyms': ['tex2d'], 'm_TextureType': 0, 'm_NormalMapSpace': 0,
                           'm_EnableGlobalMipBias': True, 'm_MipSamplingMode': 0}, group, w=208.0, h=435.0)

    # ---------- report ----------
    def describe(self):
        out = []
        props = self.properties()
        out.append(f"properties ({len(props)}):")
        for n, p in props.items():
            out.append(f"  {n:22} {p['m_Type'].split('.')[-1]:24} ref={p.get('m_OverrideReferenceName') or p['m_DefaultReferenceName']} default={p['m_Value']}")
        out.append(f"keywords: {[self.by[k['m_Id']]['m_OverrideReferenceName'] for k in self.g['m_Keywords']]}")
        out.append(f"groups: {[self.by[gd['m_Id']]['m_Title'] for gd in self.g['m_GroupDatas']]}")
        wired = {(e['m_InputSlot']['m_Node']['m_Id'], e['m_InputSlot']['m_SlotId']) for e in self.g['m_Edges']}
        out.append(f"nodes ({len(self.g['m_Nodes'])}), edges ({len(self.g['m_Edges'])}):")
        for o in self.nodes():
            label = o.get('m_FunctionName') or o.get('m_SerializedDescriptor') or o['m_Name']
            if o['m_Type'].endswith('PropertyNode'):
                label = 'Property: ' + self.by[o['m_Property']['m_Id']]['m_Name']
            out.append(f"  {o['m_Type'].split('.')[-1]:20} {label}")
            if o['m_Type'].endswith('CustomFunctionNode'):
                for s in self.slots(o):
                    io = 'out' if s['m_SlotType'] == 1 else ('wired' if (o['m_ObjectId'], s['m_Id']) in wired else f"= {s.get('m_Value')}")
                    out.append(f"      [{s['m_Id']}] {s['m_DisplayName']:20} {s['m_Type'].split('.')[-1]:22} {io}")
        return '\n'.join(out)


if __name__ == '__main__':
    if len(sys.argv) < 3 or sys.argv[2] != 'list':
        print(__doc__); sys.exit(1)
    print(Graph.load(sys.argv[1]).describe())
