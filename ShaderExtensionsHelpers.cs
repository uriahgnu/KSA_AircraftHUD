using Brutal;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using Brutal.VulkanApi.Abstractions;
using KSA;
using System;
using System.Collections.Generic;
using System.Text;

// Needs to be in ShaderExtensions namespace for shader buffer generation to work
// SxImGui can be in the normal namespace
namespace ShaderExtensions
{
    // Needed for shader buffers
    //[AttributeUsage(AttributeTargets.Struct)]
    //internal class SxUniformBufferAttribute(string xmlElement) : Attribute;

    //[AttributeUsage(AttributeTargets.Field)]
    //internal class SxUniformBufferLookupAttribute() : Attribute;

    //[AttributeUsage(AttributeTargets.Struct)]
    //internal class SxPushConstantAttribute(string xmlElement) : Attribute;

    //[AttributeUsage(AttributeTargets.Field)]
    //internal class SxPushConstantLookupAttribute() : Attribute;

    //public delegate BufferEx MPFXBufferLookup(KeyHash hash);
    //public delegate MappedMemory MPFXMemoryLookup(KeyHash hash);
    //public delegate Span<T> MPFXSpanLookup<T>(KeyHash hash) where T : unmanaged;
    //public unsafe delegate T* MPFXPtrLookup<T>(KeyHash hash) where T : unmanaged;

    internal static class SxImGui
    {
        internal static readonly KeyHash MarkerKey = KeyHash.Make("SxImGuiShader");
        internal static unsafe void CustomShader(KeyHash key)
        {
            var data = new uint2(MarkerKey.Code, key.Code);
            ImGui.GetWindowDrawList().AddCallback(DummyCallback, (Brutal.Pointers.Ptr)(&data), ByteSize.Of<uint2>().Bytes);
        }
        private static unsafe void DummyCallback(ImDrawList* parent_list, ImDrawCmd* cmd) { }
    }
}
