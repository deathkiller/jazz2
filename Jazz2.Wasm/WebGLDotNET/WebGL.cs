using WebAssembly.Core;

namespace WebGLDotNET
{
#pragma warning disable MEN002
    public partial class WebGLContextAttributes
    {
        public bool Alpha { get; set; } = true;

        public bool Depth { get; set; } = true;

        public bool Stencil { get; set; } = false;

        public bool Antialias { get; set; } = true;

        public bool PremultipliedAlpha { get; set; } = true;

        public bool PreserveDrawingBuffer { get; set; } = false;

        public bool PreferLowPowerToHighPerformance { get; set; } = false;

        public bool FailIfMajorPerformanceCaveat { get; set; } = false;

    }

    public partial class WebGLObject
    {
    }

    public partial class WebGLBuffer : WebGLObject
    {
    }

    public partial class WebGLFramebuffer : WebGLObject
    {
    }

    public partial class WebGLProgram : WebGLObject
    {
    }

    public partial class WebGLRenderbuffer : WebGLObject
    {
    }

    public partial class WebGLShader : WebGLObject
    {
    }

    public partial class WebGLTexture : WebGLObject
    {
    }

    public partial class WebGLUniformLocation
    {
    }

    public partial class WebGLActiveInfo
    {
    }

    public partial class WebGLShaderPrecisionFormat
    {
    }

    public partial class WebGLRenderingContextBase
    {
        public const uint DEPTH_BUFFER_BIT = 0x00000100;

        public const uint STENCIL_BUFFER_BIT = 0x00000400;

        public const uint COLOR_BUFFER_BIT = 0x00004000;

        public const uint POINTS = 0x0000;

        public const uint LINES = 0x0001;

        public const uint LINE_LOOP = 0x0002;

        public const uint LINE_STRIP = 0x0003;

        public const uint TRIANGLES = 0x0004;

        public const uint TRIANGLE_STRIP = 0x0005;

        public const uint TRIANGLE_FAN = 0x0006;

        public const uint ZERO = 0;

        public const uint ONE = 1;

        public const uint SRC_COLOR = 0x0300;

        public const uint ONE_MINUS_SRC_COLOR = 0x0301;

        public const uint SRC_ALPHA = 0x0302;

        public const uint ONE_MINUS_SRC_ALPHA = 0x0303;

        public const uint DST_ALPHA = 0x0304;

        public const uint ONE_MINUS_DST_ALPHA = 0x0305;

        public const uint DST_COLOR = 0x0306;

        public const uint ONE_MINUS_DST_COLOR = 0x0307;

        public const uint SRC_ALPHA_SATURATE = 0x0308;

        public const uint FUNC_ADD = 0x8006;

        public const uint BLEND_EQUATION = 0x8009;

        public const uint BLEND_EQUATION_RGB = 0x8009;

        public const uint BLEND_EQUATION_ALPHA = 0x883D;

        public const uint FUNC_SUBTRACT = 0x800A;

        public const uint FUNC_REVERSE_SUBTRACT = 0x800B;

        public const uint BLEND_DST_RGB = 0x80C8;

        public const uint BLEND_SRC_RGB = 0x80C9;

        public const uint BLEND_DST_ALPHA = 0x80CA;

        public const uint BLEND_SRC_ALPHA = 0x80CB;

        public const uint CONSTANT_COLOR = 0x8001;

        public const uint ONE_MINUS_CONSTANT_COLOR = 0x8002;

        public const uint CONSTANT_ALPHA = 0x8003;

        public const uint ONE_MINUS_CONSTANT_ALPHA = 0x8004;

        public const uint BLEND_COLOR = 0x8005;

        public const uint ARRAY_BUFFER = 0x8892;

        public const uint ELEMENT_ARRAY_BUFFER = 0x8893;

        public const uint ARRAY_BUFFER_BINDING = 0x8894;

        public const uint ELEMENT_ARRAY_BUFFER_BINDING = 0x8895;

        public const uint STREAM_DRAW = 0x88E0;

        public const uint STATIC_DRAW = 0x88E4;

        public const uint DYNAMIC_DRAW = 0x88E8;

        public const uint BUFFER_SIZE = 0x8764;

        public const uint BUFFER_USAGE = 0x8765;

        public const uint CURRENT_VERTEX_ATTRIB = 0x8626;

        public const uint FRONT = 0x0404;

        public const uint BACK = 0x0405;

        public const uint FRONT_AND_BACK = 0x0408;

        public const uint CULL_FACE = 0x0B44;

        public const uint BLEND = 0x0BE2;

        public const uint DITHER = 0x0BD0;

        public const uint STENCIL_TEST = 0x0B90;

        public const uint DEPTH_TEST = 0x0B71;

        public const uint SCISSOR_TEST = 0x0C11;

        public const uint POLYGON_OFFSET_FILL = 0x8037;

        public const uint SAMPLE_ALPHA_TO_COVERAGE = 0x809E;

        public const uint SAMPLE_COVERAGE = 0x80A0;

        public const uint NO_ERROR = 0;

        public const uint INVALID_ENUM = 0x0500;

        public const uint INVALID_VALUE = 0x0501;

        public const uint INVALID_OPERATION = 0x0502;

        public const uint OUT_OF_MEMORY = 0x0505;

        public const uint CW = 0x0900;

        public const uint CCW = 0x0901;

        public const uint LINE_WIDTH = 0x0B21;

        public const uint ALIASED_POINT_SIZE_RANGE = 0x846D;

        public const uint ALIASED_LINE_WIDTH_RANGE = 0x846E;

        public const uint CULL_FACE_MODE = 0x0B45;

        public const uint FRONT_FACE = 0x0B46;

        public const uint DEPTH_RANGE = 0x0B70;

        public const uint DEPTH_WRITEMASK = 0x0B72;

        public const uint DEPTH_CLEAR_VALUE = 0x0B73;

        public const uint DEPTH_FUNC = 0x0B74;

        public const uint STENCIL_CLEAR_VALUE = 0x0B91;

        public const uint STENCIL_FUNC = 0x0B92;

        public const uint STENCIL_FAIL = 0x0B94;

        public const uint STENCIL_PASS_DEPTH_FAIL = 0x0B95;

        public const uint STENCIL_PASS_DEPTH_PASS = 0x0B96;

        public const uint STENCIL_REF = 0x0B97;

        public const uint STENCIL_VALUE_MASK = 0x0B93;

        public const uint STENCIL_WRITEMASK = 0x0B98;

        public const uint STENCIL_BACK_FUNC = 0x8800;

        public const uint STENCIL_BACK_FAIL = 0x8801;

        public const uint STENCIL_BACK_PASS_DEPTH_FAIL = 0x8802;

        public const uint STENCIL_BACK_PASS_DEPTH_PASS = 0x8803;

        public const uint STENCIL_BACK_REF = 0x8CA3;

        public const uint STENCIL_BACK_VALUE_MASK = 0x8CA4;

        public const uint STENCIL_BACK_WRITEMASK = 0x8CA5;

        public const uint VIEWPORT = 0x0BA2;

        public const uint SCISSOR_BOX = 0x0C10;

        public const uint COLOR_CLEAR_VALUE = 0x0C22;

        public const uint COLOR_WRITEMASK = 0x0C23;

        public const uint UNPACK_ALIGNMENT = 0x0CF5;

        public const uint PACK_ALIGNMENT = 0x0D05;

        public const uint MAX_TEXTURE_SIZE = 0x0D33;

        public const uint MAX_VIEWPORT_DIMS = 0x0D3A;

        public const uint SUBPIXEL_BITS = 0x0D50;

        public const uint RED_BITS = 0x0D52;

        public const uint GREEN_BITS = 0x0D53;

        public const uint BLUE_BITS = 0x0D54;

        public const uint ALPHA_BITS = 0x0D55;

        public const uint DEPTH_BITS = 0x0D56;

        public const uint STENCIL_BITS = 0x0D57;

        public const uint POLYGON_OFFSET_UNITS = 0x2A00;

        public const uint POLYGON_OFFSET_FACTOR = 0x8038;

        public const uint TEXTURE_BINDING_2D = 0x8069;

        public const uint SAMPLE_BUFFERS = 0x80A8;

        public const uint SAMPLES = 0x80A9;

        public const uint SAMPLE_COVERAGE_VALUE = 0x80AA;

        public const uint SAMPLE_COVERAGE_INVERT = 0x80AB;

        public const uint COMPRESSED_TEXTURE_FORMATS = 0x86A3;

        public const uint DONT_CARE = 0x1100;

        public const uint FASTEST = 0x1101;

        public const uint NICEST = 0x1102;

        public const uint GENERATE_MIPMAP_HINT = 0x8192;

        public const uint BYTE = 0x1400;

        public const uint UNSIGNED_BYTE = 0x1401;

        public const uint SHORT = 0x1402;

        public const uint UNSIGNED_SHORT = 0x1403;

        public const uint INT = 0x1404;

        public const uint UNSIGNED_INT = 0x1405;

        public const uint FLOAT = 0x1406;

        public const uint DEPTH_COMPONENT = 0x1902;

        public const uint ALPHA = 0x1906;

        public const uint RGB = 0x1907;

        public const uint RGBA = 0x1908;

        public const uint LUMINANCE = 0x1909;

        public const uint LUMINANCE_ALPHA = 0x190A;

        public const uint UNSIGNED_SHORT_4_4_4_4 = 0x8033;

        public const uint UNSIGNED_SHORT_5_5_5_1 = 0x8034;

        public const uint UNSIGNED_SHORT_5_6_5 = 0x8363;

        public const uint FRAGMENT_SHADER = 0x8B30;

        public const uint VERTEX_SHADER = 0x8B31;

        public const uint MAX_VERTEX_ATTRIBS = 0x8869;

        public const uint MAX_VERTEX_UNIFORM_VECTORS = 0x8DFB;

        public const uint MAX_VARYING_VECTORS = 0x8DFC;

        public const uint MAX_COMBINED_TEXTURE_IMAGE_UNITS = 0x8B4D;

        public const uint MAX_VERTEX_TEXTURE_IMAGE_UNITS = 0x8B4C;

        public const uint MAX_TEXTURE_IMAGE_UNITS = 0x8872;

        public const uint MAX_FRAGMENT_UNIFORM_VECTORS = 0x8DFD;

        public const uint SHADER_TYPE = 0x8B4F;

        public const uint DELETE_STATUS = 0x8B80;

        public const uint LINK_STATUS = 0x8B82;

        public const uint VALIDATE_STATUS = 0x8B83;

        public const uint ATTACHED_SHADERS = 0x8B85;

        public const uint ACTIVE_UNIFORMS = 0x8B86;

        public const uint ACTIVE_ATTRIBUTES = 0x8B89;

        public const uint SHADING_LANGUAGE_VERSION = 0x8B8C;

        public const uint CURRENT_PROGRAM = 0x8B8D;

        public const uint NEVER = 0x0200;

        public const uint LESS = 0x0201;

        public const uint EQUAL = 0x0202;

        public const uint LEQUAL = 0x0203;

        public const uint GREATER = 0x0204;

        public const uint NOTEQUAL = 0x0205;

        public const uint GEQUAL = 0x0206;

        public const uint ALWAYS = 0x0207;

        public const uint KEEP = 0x1E00;

        public const uint REPLACE = 0x1E01;

        public const uint INCR = 0x1E02;

        public const uint DECR = 0x1E03;

        public const uint INVERT = 0x150A;

        public const uint INCR_WRAP = 0x8507;

        public const uint DECR_WRAP = 0x8508;

        public const uint VENDOR = 0x1F00;

        public const uint RENDERER = 0x1F01;

        public const uint VERSION = 0x1F02;

        public const uint NEAREST = 0x2600;

        public const uint LINEAR = 0x2601;

        public const uint NEAREST_MIPMAP_NEAREST = 0x2700;

        public const uint LINEAR_MIPMAP_NEAREST = 0x2701;

        public const uint NEAREST_MIPMAP_LINEAR = 0x2702;

        public const uint LINEAR_MIPMAP_LINEAR = 0x2703;

        public const uint TEXTURE_MAG_FILTER = 0x2800;

        public const uint TEXTURE_MIN_FILTER = 0x2801;

        public const uint TEXTURE_WRAP_S = 0x2802;

        public const uint TEXTURE_WRAP_T = 0x2803;

        public const uint TEXTURE_2D = 0x0DE1;

        public const uint TEXTURE = 0x1702;

        public const uint TEXTURE_CUBE_MAP = 0x8513;

        public const uint TEXTURE_BINDING_CUBE_MAP = 0x8514;

        public const uint TEXTURE_CUBE_MAP_POSITIVE_X = 0x8515;

        public const uint TEXTURE_CUBE_MAP_NEGATIVE_X = 0x8516;

        public const uint TEXTURE_CUBE_MAP_POSITIVE_Y = 0x8517;

        public const uint TEXTURE_CUBE_MAP_NEGATIVE_Y = 0x8518;

        public const uint TEXTURE_CUBE_MAP_POSITIVE_Z = 0x8519;

        public const uint TEXTURE_CUBE_MAP_NEGATIVE_Z = 0x851A;

        public const uint MAX_CUBE_MAP_TEXTURE_SIZE = 0x851C;

        public const uint TEXTURE0 = 0x84C0;

        public const uint TEXTURE1 = 0x84C1;

        public const uint TEXTURE2 = 0x84C2;

        public const uint TEXTURE3 = 0x84C3;

        public const uint TEXTURE4 = 0x84C4;

        public const uint TEXTURE5 = 0x84C5;

        public const uint TEXTURE6 = 0x84C6;

        public const uint TEXTURE7 = 0x84C7;

        public const uint TEXTURE8 = 0x84C8;

        public const uint TEXTURE9 = 0x84C9;

        public const uint TEXTURE10 = 0x84CA;

        public const uint TEXTURE11 = 0x84CB;

        public const uint TEXTURE12 = 0x84CC;

        public const uint TEXTURE13 = 0x84CD;

        public const uint TEXTURE14 = 0x84CE;

        public const uint TEXTURE15 = 0x84CF;

        public const uint TEXTURE16 = 0x84D0;

        public const uint TEXTURE17 = 0x84D1;

        public const uint TEXTURE18 = 0x84D2;

        public const uint TEXTURE19 = 0x84D3;

        public const uint TEXTURE20 = 0x84D4;

        public const uint TEXTURE21 = 0x84D5;

        public const uint TEXTURE22 = 0x84D6;

        public const uint TEXTURE23 = 0x84D7;

        public const uint TEXTURE24 = 0x84D8;

        public const uint TEXTURE25 = 0x84D9;

        public const uint TEXTURE26 = 0x84DA;

        public const uint TEXTURE27 = 0x84DB;

        public const uint TEXTURE28 = 0x84DC;

        public const uint TEXTURE29 = 0x84DD;

        public const uint TEXTURE30 = 0x84DE;

        public const uint TEXTURE31 = 0x84DF;

        public const uint ACTIVE_TEXTURE = 0x84E0;

        public const uint REPEAT = 0x2901;

        public const uint CLAMP_TO_EDGE = 0x812F;

        public const uint MIRRORED_REPEAT = 0x8370;

        public const uint FLOAT_VEC2 = 0x8B50;

        public const uint FLOAT_VEC3 = 0x8B51;

        public const uint FLOAT_VEC4 = 0x8B52;

        public const uint INT_VEC2 = 0x8B53;

        public const uint INT_VEC3 = 0x8B54;

        public const uint INT_VEC4 = 0x8B55;

        public const uint BOOL = 0x8B56;

        public const uint BOOL_VEC2 = 0x8B57;

        public const uint BOOL_VEC3 = 0x8B58;

        public const uint BOOL_VEC4 = 0x8B59;

        public const uint FLOAT_MAT2 = 0x8B5A;

        public const uint FLOAT_MAT3 = 0x8B5B;

        public const uint FLOAT_MAT4 = 0x8B5C;

        public const uint SAMPLER_2D = 0x8B5E;

        public const uint SAMPLER_CUBE = 0x8B60;

        public const uint VERTEX_ATTRIB_ARRAY_ENABLED = 0x8622;

        public const uint VERTEX_ATTRIB_ARRAY_SIZE = 0x8623;

        public const uint VERTEX_ATTRIB_ARRAY_STRIDE = 0x8624;

        public const uint VERTEX_ATTRIB_ARRAY_TYPE = 0x8625;

        public const uint VERTEX_ATTRIB_ARRAY_NORMALIZED = 0x886A;

        public const uint VERTEX_ATTRIB_ARRAY_POINTER = 0x8645;

        public const uint VERTEX_ATTRIB_ARRAY_BUFFER_BINDING = 0x889F;

        public const uint IMPLEMENTATION_COLOR_READ_TYPE = 0x8B9A;

        public const uint IMPLEMENTATION_COLOR_READ_FORMAT = 0x8B9B;

        public const uint COMPILE_STATUS = 0x8B81;

        public const uint LOW_FLOAT = 0x8DF0;

        public const uint MEDIUM_FLOAT = 0x8DF1;

        public const uint HIGH_FLOAT = 0x8DF2;

        public const uint LOW_INT = 0x8DF3;

        public const uint MEDIUM_INT = 0x8DF4;

        public const uint HIGH_INT = 0x8DF5;

        public const uint FRAMEBUFFER = 0x8D40;

        public const uint RENDERBUFFER = 0x8D41;

        public const uint RGBA4 = 0x8056;

        public const uint RGB5_A1 = 0x8057;

        public const uint RGB565 = 0x8D62;

        public const uint DEPTH_COMPONENT16 = 0x81A5;

        public const uint STENCIL_INDEX = 0x1901;

        public const uint STENCIL_INDEX8 = 0x8D48;

        public const uint DEPTH_STENCIL = 0x84F9;

        public const uint RENDERBUFFER_WIDTH = 0x8D42;

        public const uint RENDERBUFFER_HEIGHT = 0x8D43;

        public const uint RENDERBUFFER_INTERNAL_FORMAT = 0x8D44;

        public const uint RENDERBUFFER_RED_SIZE = 0x8D50;

        public const uint RENDERBUFFER_GREEN_SIZE = 0x8D51;

        public const uint RENDERBUFFER_BLUE_SIZE = 0x8D52;

        public const uint RENDERBUFFER_ALPHA_SIZE = 0x8D53;

        public const uint RENDERBUFFER_DEPTH_SIZE = 0x8D54;

        public const uint RENDERBUFFER_STENCIL_SIZE = 0x8D55;

        public const uint FRAMEBUFFER_ATTACHMENT_OBJECT_TYPE = 0x8CD0;

        public const uint FRAMEBUFFER_ATTACHMENT_OBJECT_NAME = 0x8CD1;

        public const uint FRAMEBUFFER_ATTACHMENT_TEXTURE_LEVEL = 0x8CD2;

        public const uint FRAMEBUFFER_ATTACHMENT_TEXTURE_CUBE_MAP_FACE = 0x8CD3;

        public const uint COLOR_ATTACHMENT0 = 0x8CE0;

        public const uint DEPTH_ATTACHMENT = 0x8D00;

        public const uint STENCIL_ATTACHMENT = 0x8D20;

        public const uint DEPTH_STENCIL_ATTACHMENT = 0x821A;

        public const uint NONE = 0;

        public const uint FRAMEBUFFER_COMPLETE = 0x8CD5;

        public const uint FRAMEBUFFER_INCOMPLETE_ATTACHMENT = 0x8CD6;

        public const uint FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT = 0x8CD7;

        public const uint FRAMEBUFFER_INCOMPLETE_DIMENSIONS = 0x8CD9;

        public const uint FRAMEBUFFER_UNSUPPORTED = 0x8CDD;

        public const uint FRAMEBUFFER_BINDING = 0x8CA6;

        public const uint RENDERBUFFER_BINDING = 0x8CA7;

        public const uint MAX_RENDERBUFFER_SIZE = 0x84E8;

        public const uint INVALID_FRAMEBUFFER_OPERATION = 0x0506;

        public const uint UNPACK_FLIP_Y_WEBGL = 0x9240;

        public const uint UNPACK_PREMULTIPLY_ALPHA_WEBGL = 0x9241;

        public const uint CONTEXT_LOST_WEBGL = 0x9242;

        public const uint UNPACK_COLORSPACE_CONVERSION_WEBGL = 0x9243;

        public const uint BROWSER_DEFAULT_WEBGL = 0x9244;

        public WebGLContextAttributes GetContextAttributes() => Invoke<WebGLContextAttributes>("getContextAttributes");

        public bool IsContextLost() => InvokeForBasicType<bool>("isContextLost");

        public string[] GetSupportedExtensions() => InvokeForArray<string>("getSupportedExtensions");

        public object GetExtension(string name) => Invoke("getExtension", name);

        public void ActiveTexture(uint texture) => Invoke("activeTexture", texture);

        public void AttachShader(WebGLProgram program, WebGLShader shader) => Invoke("attachShader", program, shader);

        public void BindAttribLocation(WebGLProgram program, uint index, string name) => Invoke("bindAttribLocation", program, index, name);

        public void BindBuffer(uint target, WebGLBuffer buffer) => Invoke("bindBuffer", target, buffer);

        public void BindFramebuffer(uint target, WebGLFramebuffer framebuffer) => Invoke("bindFramebuffer", target, framebuffer);

        public void BindRenderbuffer(uint target, WebGLRenderbuffer renderbuffer) => Invoke("bindRenderbuffer", target, renderbuffer);

        public void BindTexture(uint target, WebGLTexture texture) => Invoke("bindTexture", target, texture);

        public void BlendColor(float red, float green, float blue, float alpha) => Invoke("blendColor", red, green, blue, alpha);

        public void BlendEquation(uint mode) => Invoke("blendEquation", mode);

        public void BlendEquationSeparate(uint modeRGB, uint modeAlpha) => Invoke("blendEquationSeparate", modeRGB, modeAlpha);

        public void BlendFunc(uint sfactor, uint dfactor) => Invoke("blendFunc", sfactor, dfactor);

        public void BlendFuncSeparate(uint srcRGB, uint dstRGB, uint srcAlpha, uint dstAlpha) => Invoke("blendFuncSeparate", srcRGB, dstRGB, srcAlpha, dstAlpha);

        public void BufferData(uint target, ulong size, uint usage) => Invoke("bufferData", target, size, usage);

        public void BufferData(uint target, ITypedArray data, uint usage) => Invoke("bufferData", target, data, usage);

        public void BufferSubData(uint target, uint offset, ITypedArray data) => Invoke("bufferSubData", target, offset, data);

        public int CheckFramebufferStatus(uint target) => InvokeForBasicType<int>("checkFramebufferStatus", target);

        public void Clear(uint mask) => Invoke("clear", mask);

        public void ClearColor(float red, float green, float blue, float alpha) => Invoke("clearColor", red, green, blue, alpha);

        public void ClearDepth(float depth) => Invoke("clearDepth", depth);

        public void ClearStencil(int s) => Invoke("clearStencil", s);

        public void ColorMask(bool red, bool green, bool blue, bool alpha) => Invoke("colorMask", red, green, blue, alpha);

        public void CompileShader(WebGLShader shader) => Invoke("compileShader", shader);

        public void CompressedTexImage2D(uint target, int level, uint internalformat, int width, int height, int border, ITypedArray data) => Invoke("compressedTexImage2D", target, level, internalformat, width, height, border, data);

        public void CompressedTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, ITypedArray data) => Invoke("compressedTexSubImage2D", target, level, xoffset, yoffset, width, height, format, data);

        public void CopyTexImage2D(uint target, int level, uint internalformat, int x, int y, int width, int height, int border) => Invoke("copyTexImage2D", target, level, internalformat, x, y, width, height, border);

        public void CopyTexSubImage2D(uint target, int level, int xoffset, int yoffset, int x, int y, int width, int height) => Invoke("copyTexSubImage2D", target, level, xoffset, yoffset, x, y, width, height);

        public WebGLBuffer CreateBuffer() => Invoke<WebGLBuffer>("createBuffer");

        public WebGLFramebuffer CreateFramebuffer() => Invoke<WebGLFramebuffer>("createFramebuffer");

        public WebGLProgram CreateProgram() => Invoke<WebGLProgram>("createProgram");

        public WebGLRenderbuffer CreateRenderbuffer() => Invoke<WebGLRenderbuffer>("createRenderbuffer");

        public WebGLShader CreateShader(uint type) => Invoke<WebGLShader>("createShader", type);

        public WebGLTexture CreateTexture() => Invoke<WebGLTexture>("createTexture");

        public void CullFace(uint mode) => Invoke("cullFace", mode);

        public void DeleteBuffer(WebGLBuffer buffer) => Invoke("deleteBuffer", buffer);

        public void DeleteFramebuffer(WebGLFramebuffer framebuffer) => Invoke("deleteFramebuffer", framebuffer);

        public void DeleteProgram(WebGLProgram program) => Invoke("deleteProgram", program);

        public void DeleteRenderbuffer(WebGLRenderbuffer renderbuffer) => Invoke("deleteRenderbuffer", renderbuffer);

        public void DeleteShader(WebGLShader shader) => Invoke("deleteShader", shader);

        public void DeleteTexture(WebGLTexture texture) => Invoke("deleteTexture", texture);

        public void DepthFunc(uint func) => Invoke("depthFunc", func);

        public void DepthMask(bool flag) => Invoke("depthMask", flag);

        public void DepthRange(float zNear, float zFar) => Invoke("depthRange", zNear, zFar);

        public void DetachShader(WebGLProgram program, WebGLShader shader) => Invoke("detachShader", program, shader);

        public void Disable(uint cap) => Invoke("disable", cap);

        public void DisableVertexAttribArray(int index) => Invoke("disableVertexAttribArray", index);

        public void DrawArrays(uint mode, int first, int count) => Invoke("drawArrays", mode, first, count);

        public void DrawElements(uint mode, int count, uint type, uint offset) => Invoke("drawElements", mode, count, type, offset);

        public void Enable(uint cap) => Invoke("enable", cap);

        public void EnableVertexAttribArray(uint index) => Invoke("enableVertexAttribArray", index);

        public void Finish() => Invoke("finish");

        public void Flush() => Invoke("flush");

        public void FramebufferRenderbuffer(uint target, uint attachment, uint renderbuffertarget, WebGLRenderbuffer renderbuffer) => Invoke("framebufferRenderbuffer", target, attachment, renderbuffertarget, renderbuffer);

        public void FramebufferTexture2D(uint target, uint attachment, uint textarget, WebGLTexture texture, int level) => Invoke("framebufferTexture2D", target, attachment, textarget, texture, level);

        public void FrontFace(uint mode) => Invoke("frontFace", mode);

        public void GenerateMipmap(uint target) => Invoke("generateMipmap", target);

        public WebGLActiveInfo GetActiveAttrib(WebGLProgram program, uint index) => Invoke<WebGLActiveInfo>("getActiveAttrib", program, index);

        public WebGLActiveInfo GetActiveUniform(WebGLProgram program, uint index) => Invoke<WebGLActiveInfo>("getActiveUniform", program, index);

        public WebGLShader[] GetAttachedShaders(WebGLProgram program) => InvokeForArray<WebGLShader>("getAttachedShaders", program);

        public int GetAttribLocation(WebGLProgram program, string name) => InvokeForBasicType<int>("getAttribLocation", program, name);

        public object GetBufferParameter(uint target, uint pname) => Invoke("getBufferParameter", target, pname);

        public object GetParameter(uint pname) => Invoke("getParameter", pname);

        public int GetError() => InvokeForBasicType<int>("getError");

        public object GetFramebufferAttachmentParameter(uint target, uint attachment, uint pname) => Invoke("getFramebufferAttachmentParameter", target, attachment, pname);

        public object GetProgramParameter(WebGLProgram program, uint pname) => Invoke("getProgramParameter", program, pname);

        public string GetProgramInfoLog(WebGLProgram program) => InvokeForBasicType<string>("getProgramInfoLog", program);

        public object GetRenderbufferParameter(uint target, uint pname) => Invoke("getRenderbufferParameter", target, pname);

        public object GetShaderParameter(WebGLShader shader, uint pname) => Invoke("getShaderParameter", shader, pname);

        public WebGLShaderPrecisionFormat GetShaderPrecisionFormat(uint shadertype, uint precisiontype) => Invoke<WebGLShaderPrecisionFormat>("getShaderPrecisionFormat", shadertype, precisiontype);

        public string GetShaderInfoLog(WebGLShader shader) => InvokeForBasicType<string>("getShaderInfoLog", shader);

        public string GetShaderSource(WebGLShader shader) => InvokeForBasicType<string>("getShaderSource", shader);

        public object GetTexParameter(uint target, uint pname) => Invoke("getTexParameter", target, pname);

        public object GetUniform(WebGLProgram program, WebGLUniformLocation location) => Invoke("getUniform", program, location);

        public WebGLUniformLocation GetUniformLocation(WebGLProgram program, string name) => Invoke<WebGLUniformLocation>("getUniformLocation", program, name);

        public object GetVertexAttrib(uint index, uint pname) => Invoke("getVertexAttrib", index, pname);

        public ulong GetVertexAttribOffset(uint index, uint pname) => InvokeForBasicType<ulong>("getVertexAttribOffset", index, pname);

        public void Hint(uint target, uint mode) => Invoke("hint", target, mode);

        public bool IsBuffer(WebGLBuffer buffer) => InvokeForBasicType<bool>("isBuffer", buffer);

        public bool IsEnabled(uint cap) => InvokeForBasicType<bool>("isEnabled", cap);

        public bool IsFramebuffer(WebGLFramebuffer framebuffer) => InvokeForBasicType<bool>("isFramebuffer", framebuffer);

        public bool IsProgram(WebGLProgram program) => InvokeForBasicType<bool>("isProgram", program);

        public bool IsRenderbuffer(WebGLRenderbuffer renderbuffer) => InvokeForBasicType<bool>("isRenderbuffer", renderbuffer);

        public bool IsShader(WebGLShader shader) => InvokeForBasicType<bool>("isShader", shader);

        public bool IsTexture(WebGLTexture texture) => InvokeForBasicType<bool>("isTexture", texture);

        public void LineWidth(float width) => Invoke("lineWidth", width);

        public void LinkProgram(WebGLProgram program) => Invoke("linkProgram", program);

        public void PixelStorei(uint pname, int param) => Invoke("pixelStorei", pname, param);

        public void PolygonOffset(float factor, float units) => Invoke("polygonOffset", factor, units);

        public void ReadPixels(int x, int y, int width, int height, uint format, uint type, ITypedArray pixels) => Invoke("readPixels", x, y, width, height, format, type, pixels);

        public void RenderbufferStorage(uint target, uint internalformat, int width, int height) => Invoke("renderbufferStorage", target, internalformat, width, height);

        public void SampleCoverage(float value, bool invert) => Invoke("sampleCoverage", value, invert);

        public void Scissor(int x, int y, int width, int height) => Invoke("scissor", x, y, width, height);

        public void ShaderSource(WebGLShader shader, string source) => Invoke("shaderSource", shader, source);

        public void StencilFunc(uint func, int @ref, uint mask) => Invoke("stencilFunc", func, @ref, mask);

        public void StencilFuncSeparate(uint face, uint func, int @ref, uint mask) => Invoke("stencilFuncSeparate", face, func, @ref, mask);

        public void StencilMask(uint mask) => Invoke("stencilMask", mask);

        public void StencilMaskSeparate(uint face, uint mask) => Invoke("stencilMaskSeparate", face, mask);

        public void StencilOp(uint fail, uint zfail, uint zpass) => Invoke("stencilOp", fail, zfail, zpass);

        public void StencilOpSeparate(uint face, uint fail, uint zfail, uint zpass) => Invoke("stencilOpSeparate", face, fail, zfail, zpass);

        public void TexImage2D(uint target, int level, uint internalformat, int width, int height, int border, uint format, uint type, ITypedArray pixels) => Invoke("texImage2D", target, level, internalformat, width, height, border, format, type, pixels);

        public void TexImage2D(uint target, int level, uint internalformat, uint format, uint type, object source) => Invoke("texImage2D", target, level, internalformat, format, type, source);

        public void TexParameterf(uint target, uint pname, float param) => Invoke("texParameterf", target, pname, param);

        public void TexParameteri(uint target, uint pname, int param) => Invoke("texParameteri", target, pname, param);

        public void TexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, ITypedArray pixels) => Invoke("texSubImage2D", target, level, xoffset, yoffset, width, height, format, type, pixels);

        public void TexSubImage2D(uint target, int level, int xoffset, int yoffset, uint format, uint type, object source) => Invoke("texSubImage2D", target, level, xoffset, yoffset, format, type, source);

        public void Uniform1f(WebGLUniformLocation location, float x) => Invoke("uniform1f", location, x);

        public void Uniform1fv(WebGLUniformLocation location, Float32Array v) => Invoke("uniform1fv", location, v);

        public void Uniform1fv(WebGLUniformLocation location, float[] v) => Invoke("uniform1fv", location, v);

        public void Uniform1i(WebGLUniformLocation location, int x) => Invoke("uniform1i", location, x);

        public void Uniform1iv(WebGLUniformLocation location, Int32Array v) => Invoke("uniform1iv", location, v);

        public void Uniform1iv(WebGLUniformLocation location, long[] v) => Invoke("uniform1iv", location, v);

        public void Uniform2f(WebGLUniformLocation location, float x, float y) => Invoke("uniform2f", location, x, y);

        public void Uniform2fv(WebGLUniformLocation location, Float32Array v) => Invoke("uniform2fv", location, v);

        public void Uniform2fv(WebGLUniformLocation location, float[] v) => Invoke("uniform2fv", location, v);

        public void Uniform2i(WebGLUniformLocation location, int x, int y) => Invoke("uniform2i", location, x, y);

        public void Uniform2iv(WebGLUniformLocation location, Int32Array v) => Invoke("uniform2iv", location, v);

        public void Uniform2iv(WebGLUniformLocation location, long[] v) => Invoke("uniform2iv", location, v);

        public void Uniform3f(WebGLUniformLocation location, float x, float y, float z) => Invoke("uniform3f", location, x, y, z);

        public void Uniform3fv(WebGLUniformLocation location, Float32Array v) => Invoke("uniform3fv", location, v);

        public void Uniform3fv(WebGLUniformLocation location, float[] v) => Invoke("uniform3fv", location, v);

        public void Uniform3i(WebGLUniformLocation location, int x, int y, int z) => Invoke("uniform3i", location, x, y, z);

        public void Uniform3iv(WebGLUniformLocation location, Int32Array v) => Invoke("uniform3iv", location, v);

        public void Uniform3iv(WebGLUniformLocation location, long[] v) => Invoke("uniform3iv", location, v);

        public void Uniform4f(WebGLUniformLocation location, float x, float y, float z, float w) => Invoke("uniform4f", location, x, y, z, w);

        public void Uniform4fv(WebGLUniformLocation location, Float32Array v) => Invoke("uniform4fv", location, v);

        public void Uniform4fv(WebGLUniformLocation location, float[] v) => Invoke("uniform4fv", location, v);

        public void Uniform4i(WebGLUniformLocation location, int x, int y, int z, int w) => Invoke("uniform4i", location, x, y, z, w);

        public void Uniform4iv(WebGLUniformLocation location, Int32Array v) => Invoke("uniform4iv", location, v);

        public void Uniform4iv(WebGLUniformLocation location, long[] v) => Invoke("uniform4iv", location, v);

        public void UniformMatrix2fv(WebGLUniformLocation location, bool transpose, Float32Array value) => Invoke("uniformMatrix2fv", location, transpose, value);

        public void UniformMatrix2fv(WebGLUniformLocation location, bool transpose, float[] value) => Invoke("uniformMatrix2fv", location, transpose, value);

        public void UniformMatrix3fv(WebGLUniformLocation location, bool transpose, Float32Array value) => Invoke("uniformMatrix3fv", location, transpose, value);

        public void UniformMatrix3fv(WebGLUniformLocation location, bool transpose, float[] value) => Invoke("uniformMatrix3fv", location, transpose, value);

        public void UniformMatrix4fv(WebGLUniformLocation location, bool transpose, Float32Array value) => Invoke("uniformMatrix4fv", location, transpose, value);

        public void UniformMatrix4fv(WebGLUniformLocation location, bool transpose, float[] value) => Invoke("uniformMatrix4fv", location, transpose, value);

        public void UseProgram(WebGLProgram program) => Invoke("useProgram", program);

        public void ValidateProgram(WebGLProgram program) => Invoke("validateProgram", program);

        public void VertexAttrib1f(uint indx, float x) => Invoke("vertexAttrib1f", indx, x);

        public void VertexAttrib1fv(uint indx, Float32Array values) => Invoke("vertexAttrib1fv", indx, values);

        public void VertexAttrib1fv(uint indx, float[] values) => Invoke("vertexAttrib1fv", indx, values);

        public void VertexAttrib2f(uint indx, float x, float y) => Invoke("vertexAttrib2f", indx, x, y);

        public void VertexAttrib2fv(uint indx, Float32Array values) => Invoke("vertexAttrib2fv", indx, values);

        public void VertexAttrib2fv(uint indx, float[] values) => Invoke("vertexAttrib2fv", indx, values);

        public void VertexAttrib3f(uint indx, float x, float y, float z) => Invoke("vertexAttrib3f", indx, x, y, z);

        public void VertexAttrib3fv(uint indx, Float32Array values) => Invoke("vertexAttrib3fv", indx, values);

        public void VertexAttrib3fv(uint indx, float[] values) => Invoke("vertexAttrib3fv", indx, values);

        public void VertexAttrib4f(uint indx, float x, float y, float z, float w) => Invoke("vertexAttrib4f", indx, x, y, z, w);

        public void VertexAttrib4fv(uint indx, Float32Array values) => Invoke("vertexAttrib4fv", indx, values);

        public void VertexAttrib4fv(uint indx, float[] values) => Invoke("vertexAttrib4fv", indx, values);

        public void VertexAttribPointer(uint indx, int size, uint type, bool normalized, int stride, uint offset) => Invoke("vertexAttribPointer", indx, size, type, normalized, stride, offset);

        public void Viewport(int x, int y, int width, int height) => Invoke("viewport", x, y, width, height);

    }

    public partial class WebGLRenderingContext
    {
    }

    public partial class WebGLQuery : WebGLObject
    {
    }

    public partial class WebGLSampler : WebGLObject
    {
    }

    public partial class WebGLSync : WebGLObject
    {
    }

    public partial class WebGLTransformFeedback : WebGLObject
    {
    }

    public partial class WebGLVertexArrayObject : WebGLObject
    {
    }

    public partial class WebGL2RenderingContextBase
    {
        public const uint READ_BUFFER = 0x0C02;

        public const uint UNPACK_ROW_LENGTH = 0x0CF2;

        public const uint UNPACK_SKIP_ROWS = 0x0CF3;

        public const uint UNPACK_SKIP_PIXELS = 0x0CF4;

        public const uint PACK_ROW_LENGTH = 0x0D02;

        public const uint PACK_SKIP_ROWS = 0x0D03;

        public const uint PACK_SKIP_PIXELS = 0x0D04;

        public const uint COLOR = 0x1800;

        public const uint DEPTH = 0x1801;

        public const uint STENCIL = 0x1802;

        public const uint RED = 0x1903;

        public const uint RGB8 = 0x8051;

        public const uint RGBA8 = 0x8058;

        public const uint RGB10_A2 = 0x8059;

        public const uint TEXTURE_BINDING_3D = 0x806A;

        public const uint UNPACK_SKIP_IMAGES = 0x806D;

        public const uint UNPACK_IMAGE_HEIGHT = 0x806E;

        public const uint TEXTURE_3D = 0x806F;

        public const uint TEXTURE_WRAP_R = 0x8072;

        public const uint MAX_3D_TEXTURE_SIZE = 0x8073;

        public const uint UNSIGNED_INT_2_10_10_10_REV = 0x8368;

        public const uint MAX_ELEMENTS_VERTICES = 0x80E8;

        public const uint MAX_ELEMENTS_INDICES = 0x80E9;

        public const uint TEXTURE_MIN_LOD = 0x813A;

        public const uint TEXTURE_MAX_LOD = 0x813B;

        public const uint TEXTURE_BASE_LEVEL = 0x813C;

        public const uint TEXTURE_MAX_LEVEL = 0x813D;

        public const uint MIN = 0x8007;

        public const uint MAX = 0x8008;

        public const uint DEPTH_COMPONENT24 = 0x81A6;

        public const uint MAX_TEXTURE_LOD_BIAS = 0x84FD;

        public const uint TEXTURE_COMPARE_MODE = 0x884C;

        public const uint TEXTURE_COMPARE_FUNC = 0x884D;

        public const uint CURRENT_QUERY = 0x8865;

        public const uint QUERY_RESULT = 0x8866;

        public const uint QUERY_RESULT_AVAILABLE = 0x8867;

        public const uint STREAM_READ = 0x88E1;

        public const uint STREAM_COPY = 0x88E2;

        public const uint STATIC_READ = 0x88E5;

        public const uint STATIC_COPY = 0x88E6;

        public const uint DYNAMIC_READ = 0x88E9;

        public const uint DYNAMIC_COPY = 0x88EA;

        public const uint MAX_DRAW_BUFFERS = 0x8824;

        public const uint DRAW_BUFFER0 = 0x8825;

        public const uint DRAW_BUFFER1 = 0x8826;

        public const uint DRAW_BUFFER2 = 0x8827;

        public const uint DRAW_BUFFER3 = 0x8828;

        public const uint DRAW_BUFFER4 = 0x8829;

        public const uint DRAW_BUFFER5 = 0x882A;

        public const uint DRAW_BUFFER6 = 0x882B;

        public const uint DRAW_BUFFER7 = 0x882C;

        public const uint DRAW_BUFFER8 = 0x882D;

        public const uint DRAW_BUFFER9 = 0x882E;

        public const uint DRAW_BUFFER10 = 0x882F;

        public const uint DRAW_BUFFER11 = 0x8830;

        public const uint DRAW_BUFFER12 = 0x8831;

        public const uint DRAW_BUFFER13 = 0x8832;

        public const uint DRAW_BUFFER14 = 0x8833;

        public const uint DRAW_BUFFER15 = 0x8834;

        public const uint MAX_FRAGMENT_UNIFORM_COMPONENTS = 0x8B49;

        public const uint MAX_VERTEX_UNIFORM_COMPONENTS = 0x8B4A;

        public const uint SAMPLER_3D = 0x8B5F;

        public const uint SAMPLER_2D_SHADOW = 0x8B62;

        public const uint FRAGMENT_SHADER_DERIVATIVE_HINT = 0x8B8B;

        public const uint PIXEL_PACK_BUFFER = 0x88EB;

        public const uint PIXEL_UNPACK_BUFFER = 0x88EC;

        public const uint PIXEL_PACK_BUFFER_BINDING = 0x88ED;

        public const uint PIXEL_UNPACK_BUFFER_BINDING = 0x88EF;

        public const uint FLOAT_MAT2x3 = 0x8B65;

        public const uint FLOAT_MAT2x4 = 0x8B66;

        public const uint FLOAT_MAT3x2 = 0x8B67;

        public const uint FLOAT_MAT3x4 = 0x8B68;

        public const uint FLOAT_MAT4x2 = 0x8B69;

        public const uint FLOAT_MAT4x3 = 0x8B6A;

        public const uint SRGB = 0x8C40;

        public const uint SRGB8 = 0x8C41;

        public const uint SRGB8_ALPHA8 = 0x8C43;

        public const uint COMPARE_REF_TO_TEXTURE = 0x884E;

        public const uint RGBA32F = 0x8814;

        public const uint RGB32F = 0x8815;

        public const uint RGBA16F = 0x881A;

        public const uint RGB16F = 0x881B;

        public const uint VERTEX_ATTRIB_ARRAY_INTEGER = 0x88FD;

        public const uint MAX_ARRAY_TEXTURE_LAYERS = 0x88FF;

        public const uint MIN_PROGRAM_TEXEL_OFFSET = 0x8904;

        public const uint MAX_PROGRAM_TEXEL_OFFSET = 0x8905;

        public const uint MAX_VARYING_COMPONENTS = 0x8B4B;

        public const uint TEXTURE_2D_ARRAY = 0x8C1A;

        public const uint TEXTURE_BINDING_2D_ARRAY = 0x8C1D;

        public const uint R11F_G11F_B10F = 0x8C3A;

        public const uint UNSIGNED_INT_10F_11F_11F_REV = 0x8C3B;

        public const uint RGB9_E5 = 0x8C3D;

        public const uint UNSIGNED_INT_5_9_9_9_REV = 0x8C3E;

        public const uint TRANSFORM_FEEDBACK_BUFFER_MODE = 0x8C7F;

        public const uint MAX_TRANSFORM_FEEDBACK_SEPARATE_COMPONENTS = 0x8C80;

        public const uint TRANSFORM_FEEDBACK_VARYINGS = 0x8C83;

        public const uint TRANSFORM_FEEDBACK_BUFFER_START = 0x8C84;

        public const uint TRANSFORM_FEEDBACK_BUFFER_SIZE = 0x8C85;

        public const uint TRANSFORM_FEEDBACK_PRIMITIVES_WRITTEN = 0x8C88;

        public const uint RASTERIZER_DISCARD = 0x8C89;

        public const uint MAX_TRANSFORM_FEEDBACK_INTERLEAVED_COMPONENTS = 0x8C8A;

        public const uint MAX_TRANSFORM_FEEDBACK_SEPARATE_ATTRIBS = 0x8C8B;

        public const uint INTERLEAVED_ATTRIBS = 0x8C8C;

        public const uint SEPARATE_ATTRIBS = 0x8C8D;

        public const uint TRANSFORM_FEEDBACK_BUFFER = 0x8C8E;

        public const uint TRANSFORM_FEEDBACK_BUFFER_BINDING = 0x8C8F;

        public const uint RGBA32UI = 0x8D70;

        public const uint RGB32UI = 0x8D71;

        public const uint RGBA16UI = 0x8D76;

        public const uint RGB16UI = 0x8D77;

        public const uint RGBA8UI = 0x8D7C;

        public const uint RGB8UI = 0x8D7D;

        public const uint RGBA32I = 0x8D82;

        public const uint RGB32I = 0x8D83;

        public const uint RGBA16I = 0x8D88;

        public const uint RGB16I = 0x8D89;

        public const uint RGBA8I = 0x8D8E;

        public const uint RGB8I = 0x8D8F;

        public const uint RED_INTEGER = 0x8D94;

        public const uint RGB_INTEGER = 0x8D98;

        public const uint RGBA_INTEGER = 0x8D99;

        public const uint SAMPLER_2D_ARRAY = 0x8DC1;

        public const uint SAMPLER_2D_ARRAY_SHADOW = 0x8DC4;

        public const uint SAMPLER_CUBE_SHADOW = 0x8DC5;

        public const uint UNSIGNED_INT_VEC2 = 0x8DC6;

        public const uint UNSIGNED_INT_VEC3 = 0x8DC7;

        public const uint UNSIGNED_INT_VEC4 = 0x8DC8;

        public const uint INT_SAMPLER_2D = 0x8DCA;

        public const uint INT_SAMPLER_3D = 0x8DCB;

        public const uint INT_SAMPLER_CUBE = 0x8DCC;

        public const uint INT_SAMPLER_2D_ARRAY = 0x8DCF;

        public const uint UNSIGNED_INT_SAMPLER_2D = 0x8DD2;

        public const uint UNSIGNED_INT_SAMPLER_3D = 0x8DD3;

        public const uint UNSIGNED_INT_SAMPLER_CUBE = 0x8DD4;

        public const uint UNSIGNED_INT_SAMPLER_2D_ARRAY = 0x8DD7;

        public const uint DEPTH_COMPONENT32F = 0x8CAC;

        public const uint DEPTH32F_STENCIL8 = 0x8CAD;

        public const uint FLOAT_32_UNSIGNED_INT_24_8_REV = 0x8DAD;

        public const uint FRAMEBUFFER_ATTACHMENT_COLOR_ENCODING = 0x8210;

        public const uint FRAMEBUFFER_ATTACHMENT_COMPONENT_TYPE = 0x8211;

        public const uint FRAMEBUFFER_ATTACHMENT_RED_SIZE = 0x8212;

        public const uint FRAMEBUFFER_ATTACHMENT_GREEN_SIZE = 0x8213;

        public const uint FRAMEBUFFER_ATTACHMENT_BLUE_SIZE = 0x8214;

        public const uint FRAMEBUFFER_ATTACHMENT_ALPHA_SIZE = 0x8215;

        public const uint FRAMEBUFFER_ATTACHMENT_DEPTH_SIZE = 0x8216;

        public const uint FRAMEBUFFER_ATTACHMENT_STENCIL_SIZE = 0x8217;

        public const uint FRAMEBUFFER_DEFAULT = 0x8218;

        public new const uint DEPTH_STENCIL_ATTACHMENT = 0x821A;

        public new const uint DEPTH_STENCIL = 0x84F9;

        public const uint UNSIGNED_INT_24_8 = 0x84FA;

        public const uint DEPTH24_STENCIL8 = 0x88F0;

        public const uint UNSIGNED_NORMALIZED = 0x8C17;

        public const uint DRAW_FRAMEBUFFER_BINDING = 0x8CA6;

        public const uint READ_FRAMEBUFFER = 0x8CA8;

        public const uint DRAW_FRAMEBUFFER = 0x8CA9;

        public const uint READ_FRAMEBUFFER_BINDING = 0x8CAA;

        public const uint RENDERBUFFER_SAMPLES = 0x8CAB;

        public const uint FRAMEBUFFER_ATTACHMENT_TEXTURE_LAYER = 0x8CD4;

        public const uint MAX_COLOR_ATTACHMENTS = 0x8CDF;

        public const uint COLOR_ATTACHMENT1 = 0x8CE1;

        public const uint COLOR_ATTACHMENT2 = 0x8CE2;

        public const uint COLOR_ATTACHMENT3 = 0x8CE3;

        public const uint COLOR_ATTACHMENT4 = 0x8CE4;

        public const uint COLOR_ATTACHMENT5 = 0x8CE5;

        public const uint COLOR_ATTACHMENT6 = 0x8CE6;

        public const uint COLOR_ATTACHMENT7 = 0x8CE7;

        public const uint COLOR_ATTACHMENT8 = 0x8CE8;

        public const uint COLOR_ATTACHMENT9 = 0x8CE9;

        public const uint COLOR_ATTACHMENT10 = 0x8CEA;

        public const uint COLOR_ATTACHMENT11 = 0x8CEB;

        public const uint COLOR_ATTACHMENT12 = 0x8CEC;

        public const uint COLOR_ATTACHMENT13 = 0x8CED;

        public const uint COLOR_ATTACHMENT14 = 0x8CEE;

        public const uint COLOR_ATTACHMENT15 = 0x8CEF;

        public const uint FRAMEBUFFER_INCOMPLETE_MULTISAMPLE = 0x8D56;

        public const uint MAX_SAMPLES = 0x8D57;

        public const uint HALF_FLOAT = 0x140B;

        public const uint RG = 0x8227;

        public const uint RG_INTEGER = 0x8228;

        public const uint R8 = 0x8229;

        public const uint RG8 = 0x822B;

        public const uint R16F = 0x822D;

        public const uint R32F = 0x822E;

        public const uint RG16F = 0x822F;

        public const uint RG32F = 0x8230;

        public const uint R8I = 0x8231;

        public const uint R8UI = 0x8232;

        public const uint R16I = 0x8233;

        public const uint R16UI = 0x8234;

        public const uint R32I = 0x8235;

        public const uint R32UI = 0x8236;

        public const uint RG8I = 0x8237;

        public const uint RG8UI = 0x8238;

        public const uint RG16I = 0x8239;

        public const uint RG16UI = 0x823A;

        public const uint RG32I = 0x823B;

        public const uint RG32UI = 0x823C;

        public const uint VERTEX_ARRAY_BINDING = 0x85B5;

        public const uint R8_SNORM = 0x8F94;

        public const uint RG8_SNORM = 0x8F95;

        public const uint RGB8_SNORM = 0x8F96;

        public const uint RGBA8_SNORM = 0x8F97;

        public const uint SIGNED_NORMALIZED = 0x8F9C;

        public const uint COPY_READ_BUFFER = 0x8F36;

        public const uint COPY_WRITE_BUFFER = 0x8F37;

        public const uint COPY_READ_BUFFER_BINDING = 0x8F36;

        public const uint COPY_WRITE_BUFFER_BINDING = 0x8F37;

        public const uint UNIFORM_BUFFER = 0x8A11;

        public const uint UNIFORM_BUFFER_BINDING = 0x8A28;

        public const uint UNIFORM_BUFFER_START = 0x8A29;

        public const uint UNIFORM_BUFFER_SIZE = 0x8A2A;

        public const uint MAX_VERTEX_UNIFORM_BLOCKS = 0x8A2B;

        public const uint MAX_FRAGMENT_UNIFORM_BLOCKS = 0x8A2D;

        public const uint MAX_COMBINED_UNIFORM_BLOCKS = 0x8A2E;

        public const uint MAX_UNIFORM_BUFFER_BINDINGS = 0x8A2F;

        public const uint MAX_UNIFORM_BLOCK_SIZE = 0x8A30;

        public const uint MAX_COMBINED_VERTEX_UNIFORM_COMPONENTS = 0x8A31;

        public const uint MAX_COMBINED_FRAGMENT_UNIFORM_COMPONENTS = 0x8A33;

        public const uint UNIFORM_BUFFER_OFFSET_ALIGNMENT = 0x8A34;

        public const uint ACTIVE_UNIFORM_BLOCKS = 0x8A36;

        public const uint UNIFORM_TYPE = 0x8A37;

        public const uint UNIFORM_SIZE = 0x8A38;

        public const uint UNIFORM_BLOCK_INDEX = 0x8A3A;

        public const uint UNIFORM_OFFSET = 0x8A3B;

        public const uint UNIFORM_ARRAY_STRIDE = 0x8A3C;

        public const uint UNIFORM_MATRIX_STRIDE = 0x8A3D;

        public const uint UNIFORM_IS_ROW_MAJOR = 0x8A3E;

        public const uint UNIFORM_BLOCK_BINDING = 0x8A3F;

        public const uint UNIFORM_BLOCK_DATA_SIZE = 0x8A40;

        public const uint UNIFORM_BLOCK_ACTIVE_UNIFORMS = 0x8A42;

        public const uint UNIFORM_BLOCK_ACTIVE_UNIFORM_INDICES = 0x8A43;

        public const uint UNIFORM_BLOCK_REFERENCED_BY_VERTEX_SHADER = 0x8A44;

        public const uint UNIFORM_BLOCK_REFERENCED_BY_FRAGMENT_SHADER = 0x8A46;

        public const uint INVALID_INDEX = 0xFFFFFFFF;

        public const uint MAX_VERTEX_OUTPUT_COMPONENTS = 0x9122;

        public const uint MAX_FRAGMENT_INPUT_COMPONENTS = 0x9125;

        public const uint MAX_SERVER_WAIT_TIMEOUT = 0x9111;

        public const uint OBJECT_TYPE = 0x9112;

        public const uint SYNC_CONDITION = 0x9113;

        public const uint SYNC_STATUS = 0x9114;

        public const uint SYNC_FLAGS = 0x9115;

        public const uint SYNC_FENCE = 0x9116;

        public const uint SYNC_GPU_COMMANDS_COMPLETE = 0x9117;

        public const uint UNSIGNALED = 0x9118;

        public const uint SIGNALED = 0x9119;

        public const uint ALREADY_SIGNALED = 0x911A;

        public const uint TIMEOUT_EXPIRED = 0x911B;

        public const uint CONDITION_SATISFIED = 0x911C;

        public const uint WAIT_FAILED = 0x911D;

        public const uint SYNC_FLUSH_COMMANDS_BIT = 0x00000001;

        public const uint VERTEX_ATTRIB_ARRAY_DIVISOR = 0x88FE;

        public const uint ANY_SAMPLES_PASSED = 0x8C2F;

        public const uint ANY_SAMPLES_PASSED_CONSERVATIVE = 0x8D6A;

        public const uint SAMPLER_BINDING = 0x8919;

        public const uint RGB10_A2UI = 0x906F;

        public const uint INT_2_10_10_10_REV = 0x8D9F;

        public const uint TRANSFORM_FEEDBACK = 0x8E22;

        public const uint TRANSFORM_FEEDBACK_PAUSED = 0x8E23;

        public const uint TRANSFORM_FEEDBACK_ACTIVE = 0x8E24;

        public const uint TRANSFORM_FEEDBACK_BINDING = 0x8E25;

        public const uint TEXTURE_IMMUTABLE_FORMAT = 0x912F;

        public const uint MAX_ELEMENT_INDEX = 0x8D6B;

        public const uint TEXTURE_IMMUTABLE_LEVELS = 0x82DF;

        public const long TIMEOUT_IGNORED = -1;

        public const uint MAX_CLIENT_WAIT_TIMEOUT_WEBGL = 0x9247;

        public new void BufferData(uint target, ulong size, uint usage) => Invoke("bufferData", target, size, usage);

        public void BufferData(uint target, object srcData, uint usage) => Invoke("bufferData", target, srcData, usage);

        public void BufferSubData(uint target, uint dstByteOffset, object srcData) => Invoke("bufferSubData", target, dstByteOffset, srcData);

        public void BufferData(uint target, ITypedArray srcData, uint usage, uint srcOffset, uint length) => Invoke("bufferData", target, srcData, usage, srcOffset, length);

        public void BufferSubData(uint target, uint dstByteOffset, ITypedArray srcData, uint srcOffset, uint length) => Invoke("bufferSubData", target, dstByteOffset, srcData, srcOffset, length);

        public void CopyBufferSubData(uint readTarget, uint writeTarget, uint readOffset, uint writeOffset, ulong size) => Invoke("copyBufferSubData", readTarget, writeTarget, readOffset, writeOffset, size);

        public void GetBufferSubData(uint target, uint srcByteOffset, ITypedArray dstBuffer, uint dstOffset, uint length) => Invoke("getBufferSubData", target, srcByteOffset, dstBuffer, dstOffset, length);

        public void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, uint filter) => Invoke("blitFramebuffer", srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, filter);

        public void FramebufferTextureLayer(uint target, uint attachment, WebGLTexture texture, int level, int layer) => Invoke("framebufferTextureLayer", target, attachment, texture, level, layer);

        public void InvalidateFramebuffer(uint target, uint[] attachments) => Invoke("invalidateFramebuffer", target, attachments);

        public void InvalidateSubFramebuffer(uint target, uint[] attachments, int x, int y, int width, int height) => Invoke("invalidateSubFramebuffer", target, attachments, x, y, width, height);

        public void ReadBuffer(uint src) => Invoke("readBuffer", src);

        public object GetInternalformatParameter(uint target, uint internalformat, uint pname) => Invoke("getInternalformatParameter", target, internalformat, pname);

        public void RenderbufferStorageMultisample(uint target, int samples, uint internalformat, int width, int height) => Invoke("renderbufferStorageMultisample", target, samples, internalformat, width, height);

        public void TexStorage2D(uint target, int levels, uint internalformat, int width, int height) => Invoke("texStorage2D", target, levels, internalformat, width, height);

        public void TexStorage3D(uint target, int levels, uint internalformat, int width, int height, int depth) => Invoke("texStorage3D", target, levels, internalformat, width, height, depth);

        public void TexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, uint pboOffset) => Invoke("texImage2D", target, level, internalformat, width, height, border, format, type, pboOffset);

        public void TexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, object source) => Invoke("texImage2D", target, level, internalformat, width, height, border, format, type, source);

        public void TexImage2D(uint target, int level, int internalformat, int width, int height, int border, uint format, uint type, ITypedArray srcData, uint srcOffset) => Invoke("texImage2D", target, level, internalformat, width, height, border, format, type, srcData, srcOffset);

        public void TexImage3D(uint target, int level, int internalformat, int width, int height, int depth, int border, uint format, uint type, uint pboOffset) => Invoke("texImage3D", target, level, internalformat, width, height, depth, border, format, type, pboOffset);

        public void TexImage3D(uint target, int level, int internalformat, int width, int height, int depth, int border, uint format, uint type, object source) => Invoke("texImage3D", target, level, internalformat, width, height, depth, border, format, type, source);

        public void TexImage3D(uint target, int level, int internalformat, int width, int height, int depth, int border, uint format, uint type, ITypedArray srcData) => Invoke("texImage3D", target, level, internalformat, width, height, depth, border, format, type, srcData);

        public void TexImage3D(uint target, int level, int internalformat, int width, int height, int depth, int border, uint format, uint type, ITypedArray srcData, uint srcOffset) => Invoke("texImage3D", target, level, internalformat, width, height, depth, border, format, type, srcData, srcOffset);

        public void TexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, uint pboOffset) => Invoke("texSubImage2D", target, level, xoffset, yoffset, width, height, format, type, pboOffset);

        public void TexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, object source) => Invoke("texSubImage2D", target, level, xoffset, yoffset, width, height, format, type, source);

        public void TexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, ITypedArray srcData, uint srcOffset) => Invoke("texSubImage2D", target, level, xoffset, yoffset, width, height, format, type, srcData, srcOffset);

        public void TexSubImage3D(uint target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, uint type, uint pboOffset) => Invoke("texSubImage3D", target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, pboOffset);

        public void TexSubImage3D(uint target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, uint type, object source) => Invoke("texSubImage3D", target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, source);

        public void TexSubImage3D(uint target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, uint type, ITypedArray srcData, uint srcOffset) => Invoke("texSubImage3D", target, level, xoffset, yoffset, zoffset, width, height, depth, format, type, srcData, srcOffset);

        public void CopyTexSubImage3D(uint target, int level, int xoffset, int yoffset, int zoffset, int x, int y, int width, int height) => Invoke("copyTexSubImage3D", target, level, xoffset, yoffset, zoffset, x, y, width, height);

        public void CompressedTexImage2D(uint target, int level, uint internalformat, int width, int height, int border, int imageSize, uint offset) => Invoke("compressedTexImage2D", target, level, internalformat, width, height, border, imageSize, offset);

        public void CompressedTexImage2D(uint target, int level, uint internalformat, int width, int height, int border, ITypedArray srcData, uint srcOffset, uint srcLengthOverride) => Invoke("compressedTexImage2D", target, level, internalformat, width, height, border, srcData, srcOffset, srcLengthOverride);

        public void CompressedTexImage3D(uint target, int level, uint internalformat, int width, int height, int depth, int border, int imageSize, uint offset) => Invoke("compressedTexImage3D", target, level, internalformat, width, height, depth, border, imageSize, offset);

        public void CompressedTexImage3D(uint target, int level, uint internalformat, int width, int height, int depth, int border, ITypedArray srcData, uint srcOffset, uint srcLengthOverride) => Invoke("compressedTexImage3D", target, level, internalformat, width, height, depth, border, srcData, srcOffset, srcLengthOverride);

        public void CompressedTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, int imageSize, uint offset) => Invoke("compressedTexSubImage2D", target, level, xoffset, yoffset, width, height, format, imageSize, offset);

        public void CompressedTexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, ITypedArray srcData, uint srcOffset, uint srcLengthOverride) => Invoke("compressedTexSubImage2D", target, level, xoffset, yoffset, width, height, format, srcData, srcOffset, srcLengthOverride);

        public void CompressedTexSubImage3D(uint target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, int imageSize, uint offset) => Invoke("compressedTexSubImage3D", target, level, xoffset, yoffset, zoffset, width, height, depth, format, imageSize, offset);

        public void CompressedTexSubImage3D(uint target, int level, int xoffset, int yoffset, int zoffset, int width, int height, int depth, uint format, ITypedArray srcData, uint srcOffset, uint srcLengthOverride) => Invoke("compressedTexSubImage3D", target, level, xoffset, yoffset, zoffset, width, height, depth, format, srcData, srcOffset, srcLengthOverride);

        public int GetFragDataLocation(WebGLProgram program, string name) => InvokeForBasicType<int>("getFragDataLocation", program, name);

        public void Uniform1ui(WebGLUniformLocation location, uint v0) => Invoke("uniform1ui", location, v0);

        public void Uniform2ui(WebGLUniformLocation location, uint v0, uint v1) => Invoke("uniform2ui", location, v0, v1);

        public void Uniform3ui(WebGLUniformLocation location, uint v0, uint v1, uint v2) => Invoke("uniform3ui", location, v0, v1, v2);

        public void Uniform4ui(WebGLUniformLocation location, uint v0, uint v1, uint v2, uint v3) => Invoke("uniform4ui", location, v0, v1, v2, v3);

        public void Uniform1fv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform1fv", location, data, srcOffset, srcLength);

        public void Uniform2fv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform2fv", location, data, srcOffset, srcLength);

        public void Uniform3fv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform3fv", location, data, srcOffset, srcLength);

        public void Uniform4fv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform4fv", location, data, srcOffset, srcLength);

        public void Uniform1iv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform1iv", location, data, srcOffset, srcLength);

        public void Uniform2iv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform2iv", location, data, srcOffset, srcLength);

        public void Uniform3iv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform3iv", location, data, srcOffset, srcLength);

        public void Uniform4iv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform4iv", location, data, srcOffset, srcLength);

        public void Uniform1uiv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform1uiv", location, data, srcOffset, srcLength);

        public void Uniform2uiv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform2uiv", location, data, srcOffset, srcLength);

        public void Uniform3uiv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform3uiv", location, data, srcOffset, srcLength);

        public void Uniform4uiv(WebGLUniformLocation location, object data, uint srcOffset, uint srcLength) => Invoke("uniform4uiv", location, data, srcOffset, srcLength);

        public void UniformMatrix2fv(WebGLUniformLocation location, bool transpose, object data, uint srcOffset, uint srcLength) => Invoke("uniformMatrix2fv", location, transpose, data, srcOffset, srcLength);

        public void UniformMatrix3x2fv(WebGLUniformLocation location, bool transpose, object data, uint srcOffset, uint srcLength) => Invoke("uniformMatrix3x2fv", location, transpose, data, srcOffset, srcLength);

        public void UniformMatrix4x2fv(WebGLUniformLocation location, bool transpose, object data, uint srcOffset, uint srcLength) => Invoke("uniformMatrix4x2fv", location, transpose, data, srcOffset, srcLength);

        public void UniformMatrix2x3fv(WebGLUniformLocation location, bool transpose, object data, uint srcOffset, uint srcLength) => Invoke("uniformMatrix2x3fv", location, transpose, data, srcOffset, srcLength);

        public void UniformMatrix3fv(WebGLUniformLocation location, bool transpose, object data, uint srcOffset, uint srcLength) => Invoke("uniformMatrix3fv", location, transpose, data, srcOffset, srcLength);

        public void UniformMatrix4x3fv(WebGLUniformLocation location, bool transpose, object data, uint srcOffset, uint srcLength) => Invoke("uniformMatrix4x3fv", location, transpose, data, srcOffset, srcLength);

        public void UniformMatrix2x4fv(WebGLUniformLocation location, bool transpose, object data, uint srcOffset, uint srcLength) => Invoke("uniformMatrix2x4fv", location, transpose, data, srcOffset, srcLength);

        public void UniformMatrix3x4fv(WebGLUniformLocation location, bool transpose, object data, uint srcOffset, uint srcLength) => Invoke("uniformMatrix3x4fv", location, transpose, data, srcOffset, srcLength);

        public void UniformMatrix4fv(WebGLUniformLocation location, bool transpose, object data, uint srcOffset, uint srcLength) => Invoke("uniformMatrix4fv", location, transpose, data, srcOffset, srcLength);

        public void VertexAttribI4i(uint index, int x, int y, int z, int w) => Invoke("vertexAttribI4i", index, x, y, z, w);

        public void VertexAttribI4iv(uint index, object values) => Invoke("vertexAttribI4iv", index, values);

        public void VertexAttribI4ui(uint index, uint x, uint y, uint z, uint w) => Invoke("vertexAttribI4ui", index, x, y, z, w);

        public void VertexAttribI4uiv(uint index, object values) => Invoke("vertexAttribI4uiv", index, values);

        public void VertexAttribIPointer(uint index, int size, uint type, int stride, uint offset) => Invoke("vertexAttribIPointer", index, size, type, stride, offset);

        public void VertexAttribDivisor(uint index, uint divisor) => Invoke("vertexAttribDivisor", index, divisor);

        public void DrawArraysInstanced(uint mode, int first, int count, int instanceCount) => Invoke("drawArraysInstanced", mode, first, count, instanceCount);

        public void DrawElementsInstanced(uint mode, int count, uint type, uint offset, int instanceCount) => Invoke("drawElementsInstanced", mode, count, type, offset, instanceCount);

        public void DrawRangeElements(uint mode, uint start, uint end, int count, uint type, uint offset) => Invoke("drawRangeElements", mode, start, end, count, type, offset);

        public void ReadPixels(int x, int y, int width, int height, uint format, uint type, uint offset) => Invoke("readPixels", x, y, width, height, format, type, offset);

        public void ReadPixels(int x, int y, int width, int height, uint format, uint type, ITypedArray dstData, uint dstOffset) => Invoke("readPixels", x, y, width, height, format, type, dstData, dstOffset);

        public void DrawBuffers(uint[] buffers) => Invoke("drawBuffers", buffers);

        public void ClearBufferfv(uint buffer, int drawbuffer, object values, uint srcOffset) => Invoke("clearBufferfv", buffer, drawbuffer, values, srcOffset);

        public void ClearBufferiv(uint buffer, int drawbuffer, object values, uint srcOffset) => Invoke("clearBufferiv", buffer, drawbuffer, values, srcOffset);

        public void ClearBufferuiv(uint buffer, int drawbuffer, object values, uint srcOffset) => Invoke("clearBufferuiv", buffer, drawbuffer, values, srcOffset);

        public void ClearBufferfi(uint buffer, int drawbuffer, float depth, int stencil) => Invoke("clearBufferfi", buffer, drawbuffer, depth, stencil);

        public WebGLQuery CreateQuery() => Invoke<WebGLQuery>("createQuery");

        public void DeleteQuery(WebGLQuery query) => Invoke("deleteQuery", query);

        public bool IsQuery(WebGLQuery query) => InvokeForBasicType<bool>("isQuery", query);

        public void BeginQuery(uint target, WebGLQuery query) => Invoke("beginQuery", target, query);

        public void EndQuery(uint target) => Invoke("endQuery", target);

        public WebGLQuery GetQuery(uint target, uint pname) => Invoke<WebGLQuery>("getQuery", target, pname);

        public object GetQueryParameter(WebGLQuery query, uint pname) => Invoke("getQueryParameter", query, pname);

        public WebGLSampler CreateSampler() => Invoke<WebGLSampler>("createSampler");

        public void DeleteSampler(WebGLSampler sampler) => Invoke("deleteSampler", sampler);

        public bool IsSampler(WebGLSampler sampler) => InvokeForBasicType<bool>("isSampler", sampler);

        public void BindSampler(uint unit, WebGLSampler sampler) => Invoke("bindSampler", unit, sampler);

        public void SamplerParameteri(WebGLSampler sampler, uint pname, int param) => Invoke("samplerParameteri", sampler, pname, param);

        public void SamplerParameterf(WebGLSampler sampler, uint pname, float param) => Invoke("samplerParameterf", sampler, pname, param);

        public object GetSamplerParameter(WebGLSampler sampler, uint pname) => Invoke("getSamplerParameter", sampler, pname);

        public WebGLSync FenceSync(uint condition, uint flags) => Invoke<WebGLSync>("fenceSync", condition, flags);

        public bool IsSync(WebGLSync sync) => InvokeForBasicType<bool>("isSync", sync);

        public void DeleteSync(WebGLSync sync) => Invoke("deleteSync", sync);

        public uint ClientWaitSync(WebGLSync sync, uint flags, ulong timeout) => InvokeForBasicType<uint>("clientWaitSync", sync, flags, timeout);

        public void WaitSync(WebGLSync sync, uint flags, long timeout) => Invoke("waitSync", sync, flags, timeout);

        public object GetSyncParameter(WebGLSync sync, uint pname) => Invoke("getSyncParameter", sync, pname);

        public WebGLTransformFeedback CreateTransformFeedback() => Invoke<WebGLTransformFeedback>("createTransformFeedback");

        public void DeleteTransformFeedback(WebGLTransformFeedback tf) => Invoke("deleteTransformFeedback", tf);

        public bool IsTransformFeedback(WebGLTransformFeedback tf) => InvokeForBasicType<bool>("isTransformFeedback", tf);

        public void BindTransformFeedback(uint target, WebGLTransformFeedback tf) => Invoke("bindTransformFeedback", target, tf);

        public void BeginTransformFeedback(uint primitiveMode) => Invoke("beginTransformFeedback", primitiveMode);

        public void EndTransformFeedback() => Invoke("endTransformFeedback");

        public void TransformFeedbackVaryings(WebGLProgram program, string[] varyings, uint bufferMode) => Invoke("transformFeedbackVaryings", program, varyings, bufferMode);

        public WebGLActiveInfo GetTransformFeedbackVarying(WebGLProgram program, uint index) => Invoke<WebGLActiveInfo>("getTransformFeedbackVarying", program, index);

        public void PauseTransformFeedback() => Invoke("pauseTransformFeedback");

        public void ResumeTransformFeedback() => Invoke("resumeTransformFeedback");

        public void BindBufferBase(uint target, uint index, WebGLBuffer buffer) => Invoke("bindBufferBase", target, index, buffer);

        public void BindBufferRange(uint target, uint index, WebGLBuffer buffer, uint offset, ulong size) => Invoke("bindBufferRange", target, index, buffer, offset, size);

        public object GetIndexedParameter(uint target, uint index) => Invoke("getIndexedParameter", target, index);

        public uint[] GetUniformIndices(WebGLProgram program, string[] uniformNames) => InvokeForArray<uint>("getUniformIndices", program, uniformNames);

        public object GetActiveUniforms(WebGLProgram program, uint[] uniformIndices, uint pname) => Invoke("getActiveUniforms", program, uniformIndices, pname);

        public uint GetUniformBlockIndex(WebGLProgram program, string uniformBlockName) => InvokeForBasicType<uint>("getUniformBlockIndex", program, uniformBlockName);

        public object GetActiveUniformBlockParameter(WebGLProgram program, uint uniformBlockIndex, uint pname) => Invoke("getActiveUniformBlockParameter", program, uniformBlockIndex, pname);

        public string GetActiveUniformBlockName(WebGLProgram program, uint uniformBlockIndex) => InvokeForBasicType<string>("getActiveUniformBlockName", program, uniformBlockIndex);

        public void UniformBlockBinding(WebGLProgram program, uint uniformBlockIndex, uint uniformBlockBinding) => Invoke("uniformBlockBinding", program, uniformBlockIndex, uniformBlockBinding);

        public WebGLVertexArrayObject CreateVertexArray() => Invoke<WebGLVertexArrayObject>("createVertexArray");

        public void DeleteVertexArray(WebGLVertexArrayObject vertexArray) => Invoke("deleteVertexArray", vertexArray);

        public bool IsVertexArray(WebGLVertexArrayObject vertexArray) => InvokeForBasicType<bool>("isVertexArray", vertexArray);

        public void BindVertexArray(WebGLVertexArrayObject array) => Invoke("bindVertexArray", array);

    }

    public partial class WebGL2RenderingContext
    {
    }

#pragma warning restore MEN002 
}
