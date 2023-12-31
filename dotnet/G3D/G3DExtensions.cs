﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ara3D
{
    public static class G3DExtensions
    {
        public static int ElementCount(this IAttribute x)
            => x.DataCount / x.Descriptor.DataArity;

        public static IAttribute ToAttribute(this IBuffer buffer, AttributeDescriptor desc)
            => buffer.Bytes.ToAttribute(desc);

        public static IAttribute ToAttribute(this Span<byte> data, AttributeDescriptor desc)
        { 
            switch (desc.DataType)
            {
                case DataType.dt_int8:
                    return data.Cast<byte>().ToAttribute(desc);
                case DataType.dt_int16:
                    return data.Cast<short>().ToAttribute(desc);
                case DataType.dt_int32:
                    return data.Cast<int>().ToAttribute(desc);
                case DataType.dt_int64:
                    return data.Cast<long>().ToAttribute(desc);
                case DataType.dt_float32:
                    return data.Cast<float>().ToAttribute(desc);
                case DataType.dt_float64:
                    return data.Cast<double>().ToAttribute(desc);
            }
            throw new Exception($"{desc.DataType} is not a valid data type");
        }

        public static IAttribute ToAttribute<T>(this Span<T> data, AttributeDescriptor desc) where T : struct
            => data.ToArray().ToAttribute(desc);

        public static IAttribute ToAttribute<T>(this IArray<T> xs, AttributeDescriptor desc) where T: struct 
            => new AttributeArray<T>(xs, desc);

        public static IAttribute ToAttribute<T>(this T[] data, AttributeDescriptor desc) where T : struct
            => data.ToIArray().ToAttribute(desc);

        public static IAttribute ToAttribute<T>(this T[] data, Association assoc, AttributeType at, int index = 0, int data_arity = 1) where T : struct
            => data.ToAttribute(Descriptor<T>(assoc, at, index, data_arity));

        public static IAttribute ToAttribute<T>(this IArray<T> data, Association assoc, AttributeType at, int index = 0, int data_arity = 1) where T : struct
            => data.ToAttribute(Descriptor<T>(assoc, at, index, data_arity));

        public static IAttribute ToFaceSizeAttribute(this int data, Association association = Association.assoc_object)
            => new[] {data}.ToAttribute(association, AttributeType.attr_facesize);

        public static IAttribute ToFaceSizeAttribute(this int[] data, Association association = Association.assoc_object)
            => data.ToAttribute(association, AttributeType.attr_facesize);

        public static IAttribute ToFaceSizeAttribute(this IArray<int> data, Association association = Association.assoc_object)
            => data.ToAttribute(association, AttributeType.attr_facesize);

        public static IAttribute ToVertexAttribute(this Vector3[] data)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_vertex);

        public static IAttribute ToVertexAttribute(this float[] data)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_vertex, 0, 3);
        public static IAttribute ToVertexAttribute(this IArray<float> data)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_vertex, 0, 3);
        public static IAttribute ToVertexAttribute(this IArray<double> data)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_vertex, 0, 3);

        public static IAttribute ToVertexAttribute(this IArray<Vector3> data)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_vertex);

        public static IAttribute ToVertexAttribute(this DVector3[] data)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_vertex);

        public static IAttribute ToVertexAttribute(this IArray<DVector3> data)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_vertex);

        public static IAttribute ToIndexAttribute(this int[] data)
            => data.ToAttribute(Association.assoc_corner, AttributeType.attr_index);

        public static IAttribute ToIndexAttribute(this IArray<int> data)
            => data.ToAttribute(Association.assoc_corner, AttributeType.attr_index);

        public static IAttribute ToIndexAttribute(this short[] data)
            => data.ToAttribute(Association.assoc_corner, AttributeType.attr_index);

        public static IAttribute ToIndexAttribute(this IArray<short> data)
            => data.ToAttribute(Association.assoc_corner, AttributeType.attr_index);

        public static IAttribute ToIndexAttribute(this byte[] data)
            => data.ToAttribute(Association.assoc_corner, AttributeType.attr_index);

        public static IAttribute ToIndexAttribute(this IArray<byte> data)
            => data.ToAttribute(Association.assoc_corner, AttributeType.attr_index);

        public static IAttribute ToUvAttribute(this Vector2[] data, int index = 0)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_uv, index);

        public static IAttribute ToUvAttribute(this IArray<Vector2> data, int index = 0)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_uv, index);

        public static IAttribute ToUvwAttribute(this Vector3[] data, int index = 0)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_uv, index);

        public static IAttribute ToUvwAttribute(this IArray<Vector3> data, int index = 0)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_uv, index);

        public static IAttribute ToMaterialIdsAttribute(this IArray<int> data, int index = 0)
            => data.ToAttribute(Association.assoc_face, AttributeType.attr_materialid, index);

        public static IAttribute ToObjectIdsAttribute(this IArray<int> data, int index = 0)
            => data.ToAttribute(Association.assoc_face, AttributeType.attr_object_id, index);

        public static IAttribute ToGroupMaterialIdsAttribute(this IArray<int> data, int index = 0)
            => data.ToAttribute(Association.assoc_group, AttributeType.attr_materialid, index);

        public static IAttribute ToGroupObjectIdsAttribute(this IArray<int> data, int index = 0)
            => data.ToAttribute(Association.assoc_group, AttributeType.attr_object_id, index);
        public static IAttribute ToVertexNormalAttribute(this Vector3[] data, int index = 0)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_normal, index);
        public static IAttribute ToVertexNormalAttribute(this IArray<Vector3> data, int index = 0)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_normal, index);
        public static IAttribute ToVertexNormalAttribute(this IArray<float> data, int index = 0)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_normal, index, 3);
        public static IAttribute ToVertexNormalAttribute(this IArray<double> data, int index = 0)
            => data.ToAttribute(Association.assoc_vertex, AttributeType.attr_normal, index, 3);

        public static IAttribute ToInstanceTransformAttribute(this IArray<Matrix4x4> data, int index = 0)
            => data.ToAttribute(Association.assoc_instance, AttributeType.attr_instance_transform, index);

        public static IAttribute ToInstanceGroupAttribute(this IArray<int> data, int index = 0)
            => data.ToAttribute(Association.assoc_instance, AttributeType.attr_instance_group, index);

        public static IAttribute ToGroupIndexOffsetAttribute(this IArray<int> data, int index = 0)
            => data.ToAttribute(Association.assoc_group, AttributeType.attr_group_index_offset, index);

        public static IAttribute ToGroupSizeAttribute(this IArray<int> data, int index = 0)
            => data.ToAttribute(Association.assoc_group, AttributeType.attr_group_size, index);

        public static IAttribute ToGroupVertexOffsetAttribute(this IArray<int> data, int index = 0)
            => data.ToAttribute(Association.assoc_group, AttributeType.attr_group_vertex_offset, index);

        public static IEnumerable<AttributeDescriptor> Descriptors(this IG3D g3D)
            => g3D.Attributes.Select(attr => attr.Descriptor);

        public static IEnumerable<IAttribute> FindAttributes(this IG3D g3D, Func<AttributeDescriptor, bool> predicate)
            => g3D.Attributes.Where(a => predicate(a.Descriptor));

        public static IEnumerable<IAttribute> FindAttributes(this IG3D g3D, AttributeType attributeType)
            => g3D.FindAttributes(desc => desc.AttributeType == attributeType);

        public static string AttributeTypeString(this AttributeType at)
            => AttributeDescriptor.AttributeTypeToString((int)at);        

        public static IAttribute FindAttribute(this IG3D g3D, AttributeType attributeType, bool throwIfMissing = false)
        {
            var candidates = g3D.FindAttributes(attributeType).ToList();
            if (candidates.Count > 1)
                throw new Exception($"Multiple matching attributes of type {attributeType.AttributeTypeString()}");
            if (candidates.Count == 0)
            {
                if (throwIfMissing)
                    throw new Exception($"No matching attributes of type {attributeType.AttributeTypeString()}");
                return null;
            }
            return candidates[0];
        }

        public static Type ToType(this DataType dt)
        {
            switch (dt)
            {
                case DataType.dt_int8:
                    return typeof(byte);
                case DataType.dt_int16:
                    return typeof(short);
                case DataType.dt_int32:
                    return typeof(int);
                case DataType.dt_int64:
                    return typeof(long);
                case DataType.dt_float32:
                    return typeof(float);
                case DataType.dt_float64:
                    return typeof(double);
                case DataType.dt_invalid:
                    throw new Exception("Not a valid data type");
                default:
                    throw new ArgumentOutOfRangeException(nameof(dt), dt, null);
            }
        }

        public static int TypeArity<T>()
        {
            if (typeof(T) == typeof(float))
                return 1;
            if (typeof(T) == typeof(double))
                return 1;
            if (typeof(T) == typeof(short))
                return 1;
            if (typeof(T) == typeof(byte))
                return 1;
            if (typeof(T) == typeof(int))
                return 1;
            if (typeof(T) == typeof(long))
                return 1;
            if (typeof(T) == typeof(Vector2))
                return 2;
            if (typeof(T) == typeof(Vector3))
                return 3;
            if (typeof(T) == typeof(Vector4))
                return 4;
            if (typeof(T) == typeof(DVector2))
                return 2;
            if (typeof(T) == typeof(DVector3))
                return 3;
            if (typeof(T) == typeof(DVector4))
                return 4;
            if (typeof(T) == typeof(Matrix4x4))
                return 16;
            throw new Exception($"Unhandled type {typeof(T)}");
        }

        public static DataType GetDataType<T>()
        {
            if (typeof(T) == typeof(float))
                return DataType.dt_float32;
            if (typeof(T) == typeof(double))
                return DataType.dt_float64;
            if (typeof(T) == typeof(short))
                return DataType.dt_int16;
            if (typeof(T) == typeof(byte))
                return DataType.dt_int8; 
            if (typeof(T) == typeof(int))
                return DataType.dt_int32;
            if (typeof(T) == typeof(long))
                return DataType.dt_int64;
            if (typeof(T) == typeof(Vector2))
                return DataType.dt_float32;
            if (typeof(T) == typeof(Vector3))
                return DataType.dt_float32;
            if (typeof(T) == typeof(Vector4))
                return DataType.dt_float32;
            if (typeof(T) == typeof(DVector2))
                return DataType.dt_float64;
            if (typeof(T) == typeof(DVector3))
                return DataType.dt_float64;
            if (typeof(T) == typeof(DVector4))
                return DataType.dt_float64;
            if (typeof(T) == typeof(Matrix4x4))
                return DataType.dt_float32;
            throw new Exception($"Unhandled type {typeof(T)}");
        }

        public static AttributeDescriptor Descriptor<T>(Association assoc, AttributeType at, int index = 0, int arity = 1) where T : struct
            => Descriptor(assoc, at, index, GetDataType<T>(), TypeArity<T>() * arity);

        public static AttributeDescriptor Descriptor(Association assoc, AttributeType at, int index, DataType dt, int arity)
            => new AttributeDescriptor {
                _association = (int) assoc,
                _attribute_type = (int) at,
                _attribute_type_index = index,
                _data_arity = arity,
                _data_type = (int) dt,
            };        

        // TODO: read this from 
        public static G3D ReadFromFile(string filePath) 
            => G3D.Create(File.ReadAllBytes(filePath));

        public static G3D ReadFromStream(Stream stream)
            => G3D.Create(stream.ReadAllBytes());

        public static IList<INamedBuffer> ToBuffers(this IG3D g3D)
        {
            var buffers = new List<INamedBuffer>();
            buffers.Add(G3D.DefaultHeader.ToBytesAscii().ToNamedBuffer("header"));
            var descriptors = g3D.Descriptors().ToArray();
            buffers.Add(descriptors.ToNamedBuffer("descriptors"));
            foreach (var attr in g3D.Attributes)
                buffers.Add(attr.Bytes.ToNamedBuffer(attr.Descriptor.ToString()));
            return buffers;
        }
             
        public static void WriteG3D(this IG3D g3D, string filePath) 
            => g3D.ToBuffers().ToBFastFile(filePath);
            
        public static byte[] ToBytes(this IG3D g3d)
            => g3d.ToBuffers().ToBFastBytes();

        public static IG3D ToG3D(this IEnumerable<IAttribute> attributes)
            => new G3D(attributes.WhereNotNull());

        public static IG3D ToG3D(int sidesPerFaces, params IAttribute[] attributes)
            => ToG3D(new[] {sidesPerFaces.ToFaceSizeAttribute()}.Concat(attributes));

        public static IG3D ToG3D(params IAttribute[] attributes)
            => attributes.ToG3D();

        public static IG3D ToG3D(int sidesPerFaces, IArray<Vector3> vertices, IArray<int> indices = null, IArray<Vector2> uvs = null, IArray<int> materialIds = null, IArray<int> objectIds = null)
            => ToG3D(sidesPerFaces, vertices.ToVertexAttribute(), indices?.ToIndexAttribute(), uvs?.ToUvAttribute(), materialIds?.ToMaterialIdsAttribute(), objectIds?.ToObjectIdsAttribute());

        public static IG3D ToG3D(int sidesPerFaces, Vector3[] vertices, int[] indices = null, Vector2[] uvs = null)
            => ToG3D(sidesPerFaces, vertices.ToVertexAttribute(), indices?.ToIndexAttribute(), uvs?.ToUvAttribute());

        public static bool IsSameAttribute(this AttributeDescriptor desc, AttributeDescriptor other)
            => desc.AttributeType == other.AttributeType && desc.Association == other.Association && desc.AttributeTypeIndex == other.AttributeTypeIndex;

        public static bool IsSameAttribute(this IAttribute attribute, IAttribute other)
            => attribute.Descriptor.IsSameAttribute(other.Descriptor);

        public static IEnumerable<IAttribute> AttributesExcept(this IG3D g3d, AttributeDescriptor desc)
            => g3d.Attributes.Where(attr => !attr.Descriptor.Equals(desc));

        public static IG3D ReplaceAttribute(this IG3D g3d, IAttribute attr)
            => g3d.AttributesExcept(attr.Descriptor).Concat(new[] {attr}).ToG3D();

        public static IG3D AddAttributes(this IG3D g3d, params IAttribute[] attributes)
            => g3d.Attributes.Concat(attributes).ToG3D();

        public static IEnumerable<IAttribute> Attributes(this IG3D g3d, Association assoc)
            => g3d.Attributes.Where(attr => attr.Descriptor.Association == assoc);

        public static IEnumerable<IAttribute> Attributes(this IG3D g3d, AttributeType attrType)
            => g3d.Attributes.Where(attr => attr.Descriptor.AttributeType == attrType);

        public static IEnumerable<IAttribute> Attributes(this IG3D g3d, Association assoc, AttributeType attrType)
            => g3d.Attributes.Where(attr => attr.Descriptor.Association == assoc && attr.Descriptor.AttributeType == attrType);

        public static IEnumerable<IAttribute> VertexAttributes(this IG3D g3d)
            => g3d.Attributes(Association.assoc_vertex);

        public static IEnumerable<IAttribute> CornerAttributes(this IG3D g3d)
            => g3d.Attributes(Association.assoc_corner);

        public static IEnumerable<IAttribute> FaceAttributes(this IG3D g3d)
            => g3d.Attributes(Association.assoc_face);

        public static IEnumerable<IAttribute> EdgeAttributes(this IG3D g3d)
            => g3d.Attributes(Association.assoc_edge);

        public static IEnumerable<IAttribute> ObjectAttributes(this IG3D g3d)
            => g3d.Attributes(Association.assoc_object);

        public static IEnumerable<IAttribute> NoneAttributes(this IG3D g3d)
            => g3d.Attributes(Association.assoc_none);

        public static IEnumerable<IAttribute> UVAttributes(this IG3D g3D)
            => g3D.Attributes(AttributeType.attr_uv);

        public static IEnumerable<IAttribute> NormalAttributes(this IG3D g3D)
            => g3D.Attributes(AttributeType.attr_normal);
        
        public static IEnumerable<IAttribute> InstanceTransforms(this IG3D g3D)
            => g3D.Attributes(AttributeType.attr_instance_transform);

        public static IEnumerable<IAttribute> InstanceGroups(this IG3D g3D)
            => g3D.Attributes(AttributeType.attr_instance_group);

        public static IAttribute Remap(this IAttribute attr, IArray<int> indices)
        {
            var n = attr.Descriptor.DataArity;
            switch (attr.Descriptor.DataType)
            {
                case DataType.dt_int8:
                    return attr.ToBytes().SelectGroupsByIndex(n, indices).ToAttribute(attr.Descriptor);
                case DataType.dt_int16:
                    return attr.ToShorts().SelectGroupsByIndex(n, indices).ToAttribute(attr.Descriptor);                    
                case DataType.dt_int32:
                    return attr.ToInts().SelectGroupsByIndex(n, indices).ToAttribute(attr.Descriptor);
                case DataType.dt_int64:
                    return attr.ToLongs().SelectGroupsByIndex(n, indices).ToAttribute(attr.Descriptor);
                case DataType.dt_float32:
                    return attr.ToFloats().SelectGroupsByIndex(n, indices).ToAttribute(attr.Descriptor);
                case DataType.dt_float64:
                    return attr.ToDoubles().SelectGroupsByIndex(n, indices).ToAttribute(attr.Descriptor);
            }

            throw new Exception("Not a recognized data type");
        }

        public static IArray<int> CornerVertexIndices(this IG3D g3d)
            => g3d.IndexAttribute == null
                ? g3d.VertexCount().Range()
                : g3d.IndexAttribute.ToInts();

        public static bool HasFixedFaceSize(this IG3D g3d)
            => g3d.FaceSizeAttribute == null || 
               g3d.FaceSizeAttribute.Descriptor.Association == Association.assoc_object;

        public static int FirstFaceSize(this IG3D g3d)
            => g3d.FaceSizeAttribute?.ToInts()[0] ?? 3;

        public static int FaceCount(this IG3D g3d)
            => g3d.HasFixedFaceSize()
                ? g3d.CornerVertexIndices().Count / g3d.FirstFaceSize()
                : g3d.FaceSizeAttribute.ToInts().Count;

        public static IArray<int> MaterialIds(this IG3D g3d)
            => g3d.MaterialIdAttribute?.ToInts();

        public static IArray<int> ObjectIds(this IG3D g3d)
            => g3d.FindAttribute(AttributeType.attr_object_id, false)?.ToInts();

        public static IArray<int> ObjectIds(this IG3D g3d, int index)
            => g3d.FindAttributes(AttributeType.attr_object_id).ElementAt(index).ToInts();

        public static IArray<Vector2> UVs(this IG3D g3d)
            => g3d.UVAttributes().FirstOrDefault().ToVector2s();

        public static int VertexCount(this IG3D g3d)
            => g3d.VertexAttribute.ElementCount();

        public static Dictionary<string, IAttribute> ToDictionary(this IEnumerable<IAttribute> attributes)
            => attributes.ToDictionary(attr => attr.Descriptor.ToString(), attr => attr);

        public static IArray<int> FaceSizes(this IG3D g3d)
            => g3d.HasFixedFaceSize()
                ? g3d.FaceCount().Select(i => g3d.FirstFaceSize())
                : g3d.FaceSizeAttribute.ToInts();

        public static IList<IAttribute> ValidateAssociation(this IList<IAttribute> attrs, params Association[] assocs)
        {
            foreach (var attr in attrs)
                if (!assocs.Contains(attr.Descriptor.Association))
                    throw new Exception($"Attribute {attr.Descriptor} does not have a valid association");
            return attrs;
        }

        public static IList<IAttribute> ValidateDataType(this IList<IAttribute> attrs, params DataType[] dataTypes)
        {
            foreach (var attr in attrs)
                if (!dataTypes.Contains(attr.Descriptor.DataType))
                    throw new Exception($"Attribute {attr.Descriptor} does not have a valid data type");
            return attrs;
        }

        public static IList<IAttribute> ValidateArity(this IList<IAttribute> attrs, params int[] arities)
        {
            foreach (var attr in attrs)
                if (!arities.Contains(attr.Descriptor.DataArity))
                    throw new Exception($"Attribute {attr.Descriptor} does not have a valid arity");
            return attrs;
        }

        public static IList<IAttribute> ValidateMaxOne(this IList<IAttribute> attrs)
            => attrs.Count > 1 ? throw new Exception("Expected only one attribute of the specified type") : attrs;

        public static IList<IAttribute> ValidateNone(this IList<IAttribute> attrs)
            => attrs.Count > 1 ? throw new Exception("Expected only one attribute of the specified type") : attrs;

        public static IList<IAttribute> ValidateExactlyOne(this IList<IAttribute> attrs)
            => attrs.Count != 1 ? throw new Exception("Expected exactly one attribute") : attrs;

        public static void Validate(this IG3D g3d)
        {
            // Check that no attributes are null
            var n = g3d.Attributes.Count(attr => attr == null);
            if (n > 0)
                throw new Exception("Attributes cannot be null");

            // Assure that there are no duplicates
            g3d.Attributes.ToDictionary();

            // Validate the descriptors
            foreach (var attr in g3d.Attributes)
                attr.Descriptor.Validate();

            // Compute the number of faces
            var faceCount = g3d.FaceCount();

            // Assure that there is a vertex attribute
            if (g3d.VertexAttribute.Descriptor.Association != Association.assoc_vertex)
                throw new Exception("Vertex buffer is not associated with vertex: " + g3d.VertexAttribute.Descriptor);

            if (g3d.VertexAttribute.Descriptor.DataArity != 3)
                throw new Exception("Vertices should have an arity of 3");

            if (g3d.Attributes(AttributeType.attr_vertex).Count() > 1)
                throw new Exception("There can only be one vertex data set");

            // Compute the number of vertices
            var vertexCount = g3d.VertexCount();

            // Computes the number of corners 
            var cornerCount = g3d.CornerVertexIndices().Count;

            // Compute the number of groups. Groups requires the precense of a GroupIndex attribute
            var groupCount = g3d.Attributes(Association.assoc_group).FirstOrDefault()?.ElementCount() ?? -1;

            // Compute the number of instance. The first instance channel determines the number of instances
            var instanceCount = g3d.Attributes(Association.assoc_instance).FirstOrDefault()?.ElementCount() ?? -1;

            // Check the number of items in each attribute
            foreach (var attr in g3d.Attributes)
            {
                switch (attr.Descriptor.Association)
                {
                    case Association.assoc_vertex:
                        if (attr.ElementCount() != vertexCount)
                            throw new Exception($"Attribute {attr.Descriptor} has {attr.ElementCount()} items, expected {vertexCount}");
                        break;
                    case Association.assoc_face:
                        if (attr.ElementCount() != faceCount)
                            throw new Exception($"Attribute {attr.Descriptor} has {attr.ElementCount()} items, expected {faceCount}");
                        break;
                    case Association.assoc_corner:
                        if (attr.ElementCount() != cornerCount)
                            throw new Exception($"Attribute {attr.Descriptor} has {attr.ElementCount()} items, expected {cornerCount}");
                        break;
                    case Association.assoc_edge:
                        if (attr.ElementCount() != cornerCount)
                            throw new Exception($"Attribute {attr.Descriptor} has {attr.ElementCount()} items, expected {cornerCount}");
                        break;
                    case Association.assoc_object:
                        if (attr.ElementCount() != 1)
                            throw new Exception($"Attribute {attr.Descriptor} has {attr.ElementCount()} items, expected 1");
                        break;
                    case Association.assoc_group:
                        if (attr.ElementCount() != groupCount)
                            throw new Exception($"Attribute {attr.Descriptor} has {attr.ElementCount()} items, expected {groupCount}");
                        break;
                    case Association.assoc_instance:
                        if (attr.ElementCount() != instanceCount)
                            throw new Exception($"Attribute {attr.Descriptor} has {attr.ElementCount()} items, expected {instanceCount}");
                        break;
                    case Association.assoc_none:
                        break;
                    default:
                        throw new Exception($"Attribute {attr.Descriptor} has invalid association");
                }
            }

            // The following rules are stricter than absolutely necessary, but respecting them 
            // will make coding geometry libraries easier to write on top of the thing.

            g3d.Attributes(AttributeType.attr_binormal).ToList()
                .ValidateDataType(DataType.dt_float32)
                .ValidateArity(3)
                .ValidateAssociation(Association.assoc_vertex);

            g3d.Attributes(AttributeType.attr_tangent).ToList()
                .ValidateDataType(DataType.dt_float32)
                .ValidateArity(3)
                .ValidateAssociation(Association.assoc_vertex);

            g3d.Attributes(AttributeType.attr_normal).ToList()
                .ValidateDataType(DataType.dt_float32)
                .ValidateArity(3)
                .ValidateAssociation(Association.assoc_face, Association.assoc_corner, Association.assoc_vertex);

            g3d.Attributes(AttributeType.attr_color).ToList()
                .ValidateArity(1, 3, 4)
                .ValidateAssociation(Association.assoc_face, Association.assoc_corner, Association.assoc_vertex,
                    Association.assoc_object);

            g3d.Attributes(AttributeType.attr_visibility).ToList()
                .ValidateArity(1)
                .ValidateDataType(DataType.dt_int8, DataType.dt_int32, DataType.dt_float32);

            g3d.Attributes(AttributeType.attr_faceindex).ToList()
                .ValidateArity(1)
                .ValidateDataType(DataType.dt_int32)
                .ValidateAssociation(Association.assoc_face)
                .ValidateMaxOne();

            g3d.Attributes(AttributeType.attr_facesize).ToList()
                .ValidateArity(1)
                .ValidateDataType(DataType.dt_int32)
                .ValidateAssociation(Association.assoc_face, Association.assoc_object)
                .ValidateMaxOne();

            g3d.Attributes(AttributeType.attr_index).ToList()
                .ValidateArity(1)
                .ValidateDataType(DataType.dt_int32)
                .ValidateAssociation(Association.assoc_corner)
                .ValidateMaxOne();

            g3d.Attributes(AttributeType.attr_uv).ToList()
                .ValidateArity(2, 3)
                .ValidateDataType(DataType.dt_float32)
                .ValidateAssociation(Association.assoc_vertex);

            g3d.Attributes(AttributeType.attr_invalid).ToList()
                .ValidateNone();

            g3d.Attributes(AttributeType.attr_index).ToList()
                .ValidateArity(1)
                .ValidateDataType(DataType.dt_int32)
                .ValidateAssociation(Association.assoc_corner)
                .ValidateMaxOne();

            g3d.Attributes(AttributeType.attr_mapchannel_data).ToList()
                .ValidateArity(3)
                .ValidateDataType(DataType.dt_float32)
                .ValidateAssociation(Association.assoc_none);

            g3d.Attributes(AttributeType.attr_mapchannel_index).ToList()
                .ValidateArity(1)
                .ValidateDataType(DataType.dt_int32)
                .ValidateAssociation(Association.assoc_corner);

            g3d.Attributes(AttributeType.attr_materialid).ToList()
                .ValidateArity(1)
                .ValidateDataType(DataType.dt_int32)
                .ValidateAssociation(Association.assoc_face, Association.assoc_group);

            g3d.Attributes(AttributeType.attr_pervertex).ToList()
                .ValidateArity(1)
                .ValidateDataType(DataType.dt_float32)
                .ValidateAssociation(Association.assoc_vertex);

            g3d.Attributes(AttributeType.attr_polygroup).ToList()
                .ValidateArity(1)
                .ValidateDataType(DataType.dt_int32)
                .ValidateAssociation(Association.assoc_face);

            g3d.Attributes(AttributeType.attr_smoothing).ToList()
                .ValidateArity(1)
                .ValidateDataType(DataType.dt_int32)
                .ValidateAssociation(Association.assoc_face);

            g3d.Attributes(AttributeType.attr_instance_transform).ToList()
                .ValidateArity(16)
                .ValidateDataType(DataType.dt_float32)
                .ValidateAssociation(Association.assoc_instance);

            g3d.Attributes(AttributeType.attr_vertex).ToList()
                .ValidateArity(3)
                .ValidateDataType(DataType.dt_float32, DataType.dt_float64)
                .ValidateAssociation(Association.assoc_vertex)
                .ValidateExactlyOne();
        }
    }
}