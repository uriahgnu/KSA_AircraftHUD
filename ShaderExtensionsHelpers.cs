using Brutal;
using Brutal.ImGuiApi;
using Brutal.Numerics;
using Brutal.VulkanApi.Abstractions;
using KSA;
using ShaderExtensions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

// Needs to be in ShaderExtensions namespace for shader buffer generation to work
// SxImGui can be in the normal namespace
namespace ShaderExtensions
{
     //Needed for shader buffers
    [AttributeUsage(AttributeTargets.Struct)]
    internal class SxUniformBufferAttribute(string xmlElement) : Attribute;

    [AttributeUsage(AttributeTargets.Field)]
    internal class SxUniformBufferLookupAttribute() : Attribute;

    [AttributeUsage(AttributeTargets.Struct)]
    internal class SxPushConstantAttribute(string xmlElement) : Attribute;

    [AttributeUsage(AttributeTargets.Field)]
    internal class SxPushConstantLookupAttribute() : Attribute;

    public delegate BufferEx AHBufferLookup(KeyHash hash);
    public delegate MappedMemory AHMemoryLookup(KeyHash hash);
    public delegate Span<T> AHSpanLookup<T>(KeyHash hash) where T : unmanaged;
    public unsafe delegate T* AHPtrLookup<T>(KeyHash hash) where T : unmanaged;
}

namespace AircraftHUD
{
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

    // <MyBuffer Id="MyBuf" Size="1" />, where Size is the number of sequential AircraftHUDBuffer elements in the buffer
    [SxUniformBuffer("AircraftHUDBufferAsset")]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AircraftHUDBuffer
    {
        public float V1;

        // lookup delegate fields must be static fields on the buffer element type
        // the names and specific types of these are not relevant, as long as the delegate signature matches
        // these are not all required, but you will need at least one to be able to set the uniform data
        [SxUniformBufferLookup] public static AHBufferLookup LookupBuffer;
        [SxUniformBufferLookup] public static AHMemoryLookup LookupMemory;
        [SxUniformBufferLookup] public static AHSpanLookup<AircraftHUDBuffer> LookupSpan; // gives a Span<T> of length Size
        [SxUniformBufferLookup] public static AHPtrLookup<AircraftHUDBuffer> LookupPtr; // gives T* to first element
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SxPushConstant("PPPushConstantsBufferAsset")]
    public struct PPPushConstantsBuffer
    {
        public int enabled;
        public float frame;

        // lookup delegate fields must be static fields on the buffer element type
        [SxPushConstantLookup] public static AHSpanLookup<PPPushConstantsBuffer> LookupSpan; // gives a Span<T> of length Size
    }
}