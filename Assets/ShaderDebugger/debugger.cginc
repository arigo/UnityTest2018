#pragma target 4.5


#define _DEBUG_ROOT           0
#define _DEBUG_SET_COLOR      1
#define _DEBUG_SET_NEG_COLOR  2
#define _DEBUG_SET_POS        3
#define _DEBUG_SPHERE        10
#define _DEBUG_VALUE1        11
#define _DEBUG_VALUE2        12
#define _DEBUG_VALUE3        13
#define _DEBUG_VALUE4        14
#define _DEBUG_COLOR_PATCH   15
#define _DEBUG_VECTOR        16
#define _DEBUG_DIRECTION     17


struct _DebugStruct
{
    uint root;
    uint kind;
    float4 v;
};

RWStructuredBuffer<_DebugStruct> _DebugBuf;

uint _DbgEmit(uint root, uint kind, float4 v)
{
    _DebugStruct s;
    s.root = root;
    s.kind = kind;
    s.v = v;
    uint result = _Debug.IncrementCounter();
    _DebugBuf[result] = s;
    return result;
}

uint DbgO4(float4 obj_position) { return _DbgEmit(0, _DEBUG_ROOT, float4(obj_position.xyz / obj_position.w, 55)); }
uint DbgW4(float4 world_position) { return _DbgEmit(0, _DEBUG_ROOT, float4(world_position.xyz / world_position.w, 66)); }
uint DbgC4(float4 clip_position) { return _DbgEmit(0, _DEBUG_ROOT, float4(clip_position.xyz / clip_position.w, 77)); }
uint DbgV4(float4 sv_position) { return _DbgEmit(0, _DEBUG_ROOT, float4(sv_position.xyz / sv_position.w, 88)); }
uint DbgO3(float3 obj_position) { return _DbgEmit(0, _DEBUG_ROOT, float4(obj_position, 55)); }
uint DbgW3(float3 world_position) { return _DbgEmit(0, _DEBUG_ROOT, float4(world_position, 66)); }

void DbgSetColor(uint root, float4 color) { _DbgEmit(root, _DEBUG_SET_COLOR, color); }
void DbgSetNegColor(uint root, float4 color) { _DbgEmit(root, _DEBUG_SET_NEG_COLOR, color); }

void DbgMoveO4(uint root, float4 obj_position) { _DbgEmit(root, _DEBUG_SET_POS, float4(obj_position.xyz / obj_position.w, 55)); }
void DbgMoveW4(uint root, float4 world_position) { _DbgEmit(root, _DEBUG_SET_POS, float4(world_position.xyz / world_position.w, 66)); }
void DbgMoveC4(uint root, float4 clip_position) { _DbgEmit(root, _DEBUG_SET_POS, float4(clip_position.xyz / clip_position.w, 77)); }
void DbgMoveV4(uint root, float4 sv_position) { _DbgEmit(root, _DEBUG_SET_POS, float4(sv_position.xyz / sv_position.w, 88)); }
void DbgMoveO3(uint root, float3 obj_position) { _DbgEmit(root, _DEBUG_SET_POS, float4(obj_position, 55)); }
void DbgMoveW3(uint root, float3 world_position) { _DbgEmit(root, _DEBUG_SET_POS, float4(world_position, 66)); }

void DbgSphereO1(uint root, float obj_radius) { _DbgEmit(root, _DEBUG_SPHERE, float4(obj_radius, 0, 0, 55)); }
void DbgSphereW1(uint root, float world_radius) { _DbgEmit(root, _DEBUG_SPHERE, float4(world_radius, 0, 0, 66)); }

void DbgValue1(uint root, float value) { _DbgEmit(root, _DEBUG_VALUE1, float4(value, 0, 0, 0); }
void DbgValue2(uint root, float2 value) { _DbgEmit(root, _DEBUG_VALUE2, float4(value, 0, 0); }
void DbgValue3(uint root, float3 value) { _DbgEmit(root, _DEBUG_VALUE3, float4(value, 0); }
void DbgValue4(uint root, float4 value) { _DbgEmit(root, _DEBUG_VALUE4, value); }

void DbgColorPatch(uint root, float4 color) { _DbgEmit(root, _DEBUG_COLOR_PATCH, color); }

void DbgVectorO3(uint root, float3 obj_vector) { _DbgEmit(root, _DEBUG_VECTOR, float4(obj_vector, 55); }
void DbgVectorW3(uint root, float3 world_vector) { _DbgEmit(root, _DEBUG_VECTOR, float4(world_vector, 66); }

void DbgDirectionO3(uint root, float3 obj_direction) { _DbgEmit(root, _DEBUG_DIRECTION, float4(obj_vector, 55); }
void DbgDirectionW3(uint root, float3 world_direction) { _DbgEmit(root, _DEBUG_DIRECTION, float4(world_vector, 66); }

void DbgCubeSizeO3(uint root, float3 obj_size) { _DbgEmit(root, _DEBUG_CUBE, float4(obj_size, 55); }
void DbgCubeSizeW3(uint root, float3 world_size) { _DbgEmit(root, _DEBUG_CUBE, float4(world_size, 66); }
