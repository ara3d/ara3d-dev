﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Ara3D
{
    public interface IGeometry : IG3D
    {
        int PointsPerFace { get; }
        int NumFaces { get; }
        IArray<Vector3> Vertices { get; } 
        IArray<int> Indices { get; }  
        IArray<int> FaceSizes { get; }
        IArray<Vector2> UVs { get; }

        // NOTE: not necessarily thread safe
        Topology Topology { get; }
    }

    /// <summary>
    /// This class is used to make efficient topological queries for an IGeometry.
    /// Construction is an O(N) operation, so it is not always created automatically. 
    /// </summary>
    public class Topology
    {
        public Topology(IGeometry g)
        {
            Geometry = g;
            Corners = g.Indices.Indices();
            Faces = Geometry.NumFaces.Range();
            Vertices = g.Vertices.Indices();

            // Compute the mapping from corner (indices of the index buffer) to faces
            // and the mapping from faces to the first corner in that face
            CornersToFaces = new int[g.Indices.Count];
            FacesToCorners = new int[g.NumFaces];
            var corner = 0;
            for (var f = 0; f < g.NumFaces; ++f)
            {
                FacesToCorners[f] = corner;
                var faceSize = g.FaceSizes[f];
                for (var j = 0; j < faceSize; ++j)
                    CornersToFaces[corner++] = f;
            }

            // Compute the mapping from vertex indices to faces that reference them 
            VerticesToFaces = new List<int>[g.Vertices.Count];
            for (var c = 0; c < g.Indices.Count; ++c)
            {
                var v = g.Indices[c];
                var f = CornersToFaces[c];
                if (VerticesToFaces[v] == null)
                    VerticesToFaces[v] = new List<int> { f };
                else
                    VerticesToFaces[v].Add(f);
            }

            // NOTE: a non-manifold condition can arise where the same vertex is in multiple faces.
            // I am not considering that case for now. 

            // Compute the face on the other side of an edge 
            EdgeToOtherFace = (-1).Repeat(g.Indices.Count).ToArray();
            for (var c = 0; c < g.Indices.Count; ++c)
            {
                var c2 = NextCorner(c);
                var f0 = CornerToFace(c);
                foreach (var f1 in FacesFromCorner(c))
                { 
                    if (f1 != f0)
                    { 
                        foreach (var f2 in FacesFromCorner(c2))
                        { 
                            if (f2 == f1)
                            {
                                if (EdgeToOtherFace[c] != -1)
                                    NonManifold = true;
                                EdgeToOtherFace[c] = f2;
                            }
                        }
                    }
                }
            }
        }

        public IGeometry Geometry { get; }

        public List<int>[] VerticesToFaces { get; }
        public int[] EdgeToOtherFace { get; } // Assumes manifold meshes
        public int[] CornersToFaces { get; }
        public int[] FacesToCorners { get; }
        public bool NonManifold { get; } 
        public IArray<int> Corners { get; }
        public IArray<int> Vertices { get; }
        public IArray<int> Edges => Corners;
        public IArray<int> Faces { get; }

        public int CornerToFace(int i)
            => CornersToFaces[i];

        public IEnumerable<int> FacesFromVertexIndex(int v)
            => VerticesToFaces[v] ?? Enumerable.Empty<int>();

        public IArray<int> IndexIndicesFromFace(int f)
            => Geometry.FaceSizes[f].Range().Add(FacesToCorners[f]);

        public IArray<int> VertexIndicesFromFace(int f)
            => Geometry.Indices.SelectByIndex(IndexIndicesFromFace(f));

        public IEnumerable<int> FacesFromCorner(int c)
            => FacesFromVertexIndex(Geometry.Indices[c]);

        public IEnumerable<int> NeighbourFaces(int f)
            => VertexIndicesFromFace(f).ToEnumerable().SelectMany(FacesFromVertexIndex).Where(f2 => f2 != f).Distinct();

        public int VertexIndexFromCorner(int c)
            => Geometry.Indices[c];

        public bool SharesEdge(int f1, int f2)
            => NumSharedVertices(f1, f2) >= 2;

        public int NumSharedVertices(int f1, int f2)
        {
            var set1 = VertexIndicesFromFace(f1);
            var set2 = VertexIndicesFromFace(f2);

            var cnt = 0;
            for (var i=0; i < set1.Count; i++)
                for (var j=0; j < set2.Count; j++)
                    if (set1[i] == set2[j])
                        cnt++;
            return cnt;
        }

        // Returns true if the two faces share the same topology
        public bool SameTopology(int f1, int f2)
        {
            var set1 = VertexIndicesFromFace(f1);
            var set2 = VertexIndicesFromFace(f2);
            if (set1.Count != set2.Count)
                return false;

            for (var i = 0; i < set1.Count; i++)
                if (set1[i] != set2[i])
                    return false;
            return true;
        }

        /// <summary>
        /// Differs from neighbour faces in that the faces have to share an edge, not just a vertex.
        /// An alternative construction would have been to getNeighbourFaces and filter out those that don't share
        /// </summary>
        public IEnumerable<int> BorderingFacesFromFace(int f)
            => EdgesFromFace(f).Select(BorderFace).Where(bf => bf >= 0);

        public int BorderFace(int e)
            => EdgeToOtherFace[e];

        public bool IsBorderEdge(int e)
            => EdgeToOtherFace[e] < 0;

        public bool IsBorderFace(int f)
            => EdgesFromFace(f).Any(IsBorderEdge);

        public IArray<int> CornersFromFace(int f)
            => FaceSize(f).Range().Add(FirstCornerInFace(f));

        public IArray<int> EdgesFromFace(int f)
            => CornersFromFace(f);

        public int FirstCornerInFace(int f)
            => FacesToCorners[f];

        public int FaceSize(int f)
            => Geometry.FaceSizes[f];

        public int FaceFromCorner(int c)
            => CornersToFaces[c];

        public bool FaceHasCorner(int f, int c)
            => CornersFromFace(f).Contains(c);

        public int NextCorner(int c)
        {
            var f = FaceFromCorner(c);
            var begin = FirstCornerInFace(f);
            var end = begin + FaceSize(f);
            Debug.Assert(c >= begin);
            Debug.Assert(c < end);
            var c2 = c + 1;
            if (c2 < end)
                return c2;
            Debug.Assert(c2 == end);
            return begin;
        }

        public IArray<int> CornersFromEdge(int e)
            => LinqArray.Create(e, NextCorner(e));

        public IArray<int> VertexIndicesFromEdge(int e)
            => CornersFromEdge(e).Select(VertexIndexFromCorner);

        public IEnumerable<int> NeighbourVertices(int v)
            => FacesFromVertexIndex(v).SelectMany(f => VertexIndicesFromFace(f).ToEnumerable()).Where(v2 => v2 != v).Distinct();

        public IEnumerable<int> BorderEdges
            => Edges.Where(IsBorderEdge);

        public IEnumerable<int> BorderFaces 
            => Faces.Where(IsBorderFace);

        public int EdgeFirstCorner(int e)
            => e;

        public int EdgeNextCorner(int e)
            => NextCorner(e);

        public int EdgeFirstVertex(int e)
            => VertexIndexFromCorner(EdgeFirstCorner(e));

        public int EdgeNextVertex(int e)
            => VertexIndexFromCorner(EdgeFirstCorner(e));

        public IArray<int> EdgeVertices(int e)
            => LinqArray.Create(EdgeFirstVertex(e), EdgeNextVertex(e));

        public Vector3 PointFromVertex(int v)
            => Geometry.Vertices[v];

        public IArray<Vector3> EdgePoints(int e)
            => EdgeVertices(e).Select(PointFromVertex);
    }

    public class GeometryDebugView
    {
        IGeometry Interface { get; }

        public int PointsPerFace => Interface.PointsPerFace;
        public int NumFaces => Interface.NumFaces;
        public Vector3[] Vertices => Interface.Vertices.ToArray();
        public int[] Indices => Interface.Indices.ToArray();
        public int[] FaceSizes => Interface.FaceSizes.ToArray();

        public GeometryDebugView(IGeometry g)
        {
            Interface = g;
        }
    }

    /*
    // https://www.scratchapixel.com/lessons/advanced-rendering/introduction-acceleration-structure/introduction
    // https://stackoverflow.com/questions/99796/when-to-use-binary-space-partitioning-quadtree-octree
    // http://gamma.cs.unc.edu/RS/paper_rt07.pdf
    public interface IGeometryAccelerations
    {
        Box Box { get; }
        object BVH { get; }
        object Octree { get; }
        IArray<int> VertexIndexLookup { get; }
        object BSP { get; }
        object AABBTree { get; }
        object RayStrips { get; }
    }

    public interface ICommonAttributeData
    {
        IArray<Vector2> Uvs(int n);
        IArray<Vector3> Uvws(int n);
        IArray<Vector3> Vertices { get; }
        IArray<int> Indices { get; }
        IArray<int> FaceSizes { get; }
        IArray<int> FaceToIndexBuffer { get; }
        IArray<Vector3> MapChannelData(int n);
        IArray<int> MapChannelIndices(int n);
        IArray<Vector3> FaceNormals { get; }
        IArray<Vector3> VertexNormals { get; }
        IArray<Vector3> VertexBinormals { get; }
        IArray<Vector3> VertexTangents { get; }
        IArray<int> MaterialIds { get; }
        IArray<int> PolyGroups { get; }
        IArray<float> PerVertex(int n);
        IArray<Vector3> VertexColors { get; }
        IArray<int> SmoothingGroups { get; }
        IArray<byte> EdgeVisibility { get; }
        IArray<float> FaceSelection { get; }
        IArray<float> EdgeSelection { get; }
        IArray<float> VertexSelection { get; }
    }
    */

    /// <summary>
    /// A face is an array of indices into the vertex buffer of an IGeometry representing a particular
    /// element of a geometry (could be a PolyMesh face, a TriMesh faces, a QuadMesh face, or even a line segment,
    /// or point if the IGeometry represents a point cloud. 
    /// </summary>
    public struct Face : IArray<int>, IEquatable<Face>
    {
        public IGeometry Geometry { get; }
        public int Index { get; }
        public int Count => Geometry.FaceSizes[Index];
        public int this[int n] => Geometry.Indices[Geometry.Topology.FacesToCorners[Index] + n];

        public bool HasDegenerateIndices()
        {
            for (var i=0; i < Count - 1; ++i)
                for (var j=i+1; j < Count; ++j)
                    if (this[i] == this[j])
                        return true;
            return false;
        }

        public Face(IGeometry g, int index)
        {
            Geometry = g;
            Index = index;
        }

        public override string ToString()
            => string.Join(" ", this.ToEnumerable());

        public bool Equals(Face other)
            => this.Sort().SequenceEquals(other.Sort());

        public override bool Equals(object obj)
            => obj is Face f && Equals(f);

        public override int GetHashCode()
            => Hash.Combine(this.Sort().ToArray());

        public IEnumerable<Face> NeighbourFaces()
        {
            var g = Geometry;
            return Geometry.Topology.NeighbourFaces(Index).Select(i => new Face(g, i));
        }
    }

    public static class Geometry
    {
        // Epsilon is bigger than the real epsilon. 
        public const float EPSILON = float.Epsilon * 100;

        public readonly static IGeometry EmptyTriMesh
            = TriMesh(LinqArray.Empty<Vector3>(), LinqArray.Empty<int>());

        public readonly static IGeometry EmptyQuadMesh
            = QuadMesh(LinqArray.Empty<Vector3>(), LinqArray.Empty<int>());

        public static Vector3 MidPoint(this Face self)
            => self.Points().Average();

        public static IArray<Vector3> Points(this Face self)
            => self.Geometry.Vertices.SelectByIndex(self);        

        public static int FaceCount(this IGeometry self)
            => self.GetFaces().Count;        

        /*
        public static IGeometry ToPolyMesh(this IGeometry self, IEnumerable<IEnumerable<int>> indices)
        {
            var verts = self.Vertices;
            var flatIndices = indices.SelectMany(xs => xs).ToIArray();
            var faceIndices = indices.Where(xs => xs.Any()).Select(xs => xs.First()).ToIArray();
            return new BaseMesh(verts, flatIndices, faceIndices);
        }
        */

        /*
      public static IGeometry MergeCoplanar(this IGeometry self)
      {
          if (self.Elements.ByteCount <= 1) return self;
          var curPoly = new List<int>();
          var polys = new List<List<int>> { curPoly };
          var cur = 0;
          for (var i=1; i < self.Elements.ByteCount; ++i)
          {
              if (!self.CanMergeTris(cur, i))
              {
                  cur = i;
                  polys.Add(curPoly = new List<int>());
              }
              curPoly.Add(self.Elements[i].ToList());
          }
          return self.ToPolyMesh(polys);
      }     
      */

        public static Vector3 Tangent(this Face self)
            => self.Points()[1] - self.Points()[0];
        
        public static Vector3 Binormal(this Face self)
            => self.Points()[2] - self.Points()[0];        

        public static IArray<Triangle> Triangles(this Face self)
        {
            if (self.Count < 3) return new Triangle(Vector3.Zero, Vector3.Zero, Vector3.Zero).Repeat(0);
            var pts = self.Points();
            if (self.Count == 3) return new Triangle(pts[0], pts[1], pts[2]).Repeat(1);
            return (self.Count - 2).Select(i => new Triangle(pts[0], pts[i + 1], pts[i + 2]));
        }

        // https://en.wikipedia.org/wiki/Coplanarity
        public static bool Coplanar(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, float epsilon = EPSILON)
            => Math.Abs(Vector3.Dot(v3 - v1, (v2 - v1).Cross(v4 - v1))) < epsilon;        

        public static Vector3 Normal(this Face self)
            => self.Binormal().Cross(self.Tangent()).Normalize();
        
        public static IGeometry Mesh(int sidesPerFace, IArray<Vector3> vertices, IArray<int> indices = null, IArray<Vector2> uvs = null, IArray<int> materialIds = null, IArray<int> objectIds = null)
            => G3DExtensions.ToG3D(sidesPerFace, vertices, indices, uvs, materialIds, objectIds).ToIGeometry();

        public static IGeometry QuadMesh(this IArray<Vector3> vertices, IArray<int> indices = null, IArray<Vector2> uvs = null, IArray<int> materialIds = null, IArray<int> objectIds = null)
            => Mesh(4, vertices, indices, uvs, materialIds, objectIds);

        public static IGeometry TriMesh(this IArray<Vector3> vertices, IArray<int> indices = null, IArray<Vector2> uvs = null, IArray<int> materialIds = null, IArray<int> objectIds = null)
            => Mesh(3, vertices, indices, uvs, materialIds, objectIds);

        /* TODO: finish
        public static IGeometry PolyMesh(this IArray<Vector3> vertices, IArray<Face> faces)
        {
            var vertexAttribute = vertices.ToVertexAttribute();
            var indexBuffer = faces.
        }*/

        /// <summary>
        /// Computes the indices of a quad mesh strip.
        /// TODO: support wrapping around the u or the v, so thatvertex indices are re-used if need be. Otherwise cylinders have coincident vertices 
        /// </summary>
        public static IArray<int> ComputeQuadMeshStripIndices(int usegs, int vsegs)
        {
            var indices = new List<int>();
            for (var i = 0; i < vsegs; ++i)
            {
                for (var j = 0; j < usegs; ++j)
                {
                    indices.Add(i * (usegs + 1) + j);
                    indices.Add(i * (usegs + 1) + j + 1);
                    indices.Add((i + 1) * (usegs + 1) + j + 1);
                    indices.Add((i + 1) * (usegs + 1) + j);
                }
            }

            return indices.ToIArray();
        }

        public static IGeometry QuadMesh(this Func<Vector2, Vector3> f, int usegs, int vsegs)
        {
            var verts = new List<Vector3>();
            for (var i = 0; i <= vsegs; ++i)
            {
                var v = (float) i / vsegs;
                for (var j = 0; j <= usegs; ++j)
                {
                    var u = (float) j / usegs;
                    verts.Add(f(new Vector2(u, v)));
                }
            }

            return QuadMesh(verts.ToIArray(), ComputeQuadMeshStripIndices(usegs, vsegs));
        }

        public static bool CanMergeTris(this IGeometry self, int a, int b)
        {
            var e1 = self.GetFaces()[a];
            var e2 = self.GetFaces()[b];
            if (e1.Count != e2.Count && e1.Count != 3) return false;
            var indices = new[] { e1[0], e1[1], e1[2], e2[0], e2[1], e2[2] }.Distinct().ToList();
            if (indices.Count != 4) return false;
            var verts = self.Vertices.SelectByIndex(indices.ToIArray());
            return Coplanar(verts[0], verts[1], verts[2], verts[3]);
        }

        public static IArray<Vector3> UsedVertices(this IGeometry self) 
            => self.GetFaces().SelectMany(es => es.Points());

        public static IArray<Vector3> FaceMidPoints(this IGeometry self) 
            => self.GetFaces().Select(e => e.MidPoint());

        // TODO: keep all the other data. 
        public static IGeometry WeldVertices(this IGeometry self)
        {
            var verts = new Dictionary<Vector3, int>();
            var indices = new List<int>();
            for (var i = 0; i < self.Vertices.Count; ++i)
            {
                var v = self.Vertices[i];
                if (verts.ContainsKey(v))
                {
                    indices.Add(verts[v]);
                }
                else
                {
                    var n = verts.Count;
                    indices.Add(n);
                    verts.Add(v, n);
                }
            }
            return Mesh(self.PointsPerFace, verts.Keys.ToIArray(), indices.ToIArray());
        }

        public static IGeometry SetVertices(this IGeometry self, IArray<Vector3> vertices)
            => self?.ReplaceAttribute(vertices.ToVertexAttribute())?.ToIGeometry();

        public static IGeometry Deform(this IGeometry self, Func<Vector3, Vector3> f)
            => self.SetVertices(self.Vertices.Select(f));

        public static IGeometry Deform(this IGeometry self, Func<Vector3, Vector3> f, Func<Vector3, float> weight)
            => self.SetVertices(self.Vertices.Select(v => v.Lerp(f(v), weight(v))));

        public static IGeometry Transform(this IGeometry self, Matrix4x4 m)
            => self.Deform(v => v.Transform(m));
        
        public static IGeometry Translate(this IGeometry self, Vector3 offset)
            => self.Deform(v => v + offset);

        public static IGeometry Scale(this IGeometry self, float amount)
            => self.Deform(v => v * amount);

        public static AABox BoundingBox(this IArray<Vector3> vertices)
            => AABox.Create(vertices.ToEnumerable());        

        public static AABox BoundingBox(this IGeometry self)
            => self.Vertices.BoundingBox();

        public static bool IsPolyMesh(this IGeometry self)
            => !self.HasFixedFaceSize();

        // TODO: this is being superceded by a new set of classes for analyzing geometry called GeometryAnalysis.cs
        public static string GetStats(this IGeometry self)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Points per face {self.PointsPerFace}");
            sb.AppendLine($"Is PolyMesh {self.IsPolyMesh()}");
            sb.AppendLine($"Number of vertices {self.Vertices.Count}");
            sb.AppendLine($"Number of used vertices {self.UsedVertices().Count}");
            sb.AppendLine($"Number of indices {self.Indices.Count}");
            sb.AppendLine($"Number of faces  {self.GetFaces().Count}");
            sb.AppendLine($"Bounding box {self.BoundingBox()}");
            sb.AppendLine($"Average vertex {self.Vertices.Average()}");
            // TODO: distance from ground plane (box extent)
            // TODO: closest distance to origin (from box extent)
            // TODO: standard deviation 
            // TODO: scene analysis as well 
            // TODO: number of distinct vertices 
            // TODO: volume of bounding box
            // TODO: surface area of bounding box on ground plane
            // TODO: average vertex B
            // TODO: average normal and average UV 
            // TODO: total area 
            var tris = self.Triangles();
            sb.AppendLine($"Triangles {tris.Count}");
            // TODO: this did not return actual distinct triangles and it is slow!!!
            //sb.AppendLine($"Distinct triangles {tris.ToEnumerable().Distinct().Count()}");
            var smallArea = 0.00001;
            sb.AppendLine($"Triangles with small area {tris.CountWhere(tri => tri.Area < smallArea)}");
            return sb.ToString();
        }

        public static double Area(this IGeometry g)
            => g.Triangles().Sum(t => t.Area);

        public static IArray<Triangle> Triangles(this IGeometry self)
            => self.GetFaces().SelectMany(e => e.Triangles());       

        // This assumes that every polygon is convex, and without holes. Line or point elements are not converted into triangles. 
        // TODO: move all data channels along for the ride. 
        public static IGeometry ToTriMesh(this IGeometry self)
        {
            if (self.PointsPerFace == 3)
                return self;
            var indices = new List<int>();
            for (var i = 0; i < self.NumFaces; ++i)
            {
                var e = self.GetFace(i);
                for (var j = 1; j < e.Count - 1; ++j)
                {
                    indices.Add(e[0]);
                    indices.Add(e[j]);
                    indices.Add(e[j + 1]);
                }
            }
            return TriMesh(self.Vertices, indices.ToIArray());
        }

        public static IGeometry Merge(this IEnumerable<IGeometry> geometries, bool weldVertices = false, float weldTolerance = (float)Constants.MmToFeet) 
            => geometries.ToIArray().Merge(weldVertices, weldTolerance);

        // TODO: this function need to be generalized to handle all attributes correctly. In fact I think it should proably happen at the IG3D level.
        public static IGeometry Merge(this IArray<IGeometry> geometries, bool weldVertices = false, float weldTolerance = (float)Constants.MmToFeet)
            => weldVertices ? MergeWelded(geometries, weldTolerance) : MergeUnwelded(geometries);

        public class WeldingVertexLookup
        {
            public WeldingVertexLookup(float multiplier = (float)Constants.MmToFeet)
            {
                Multiplier = multiplier;
            }

            /// <summary>
            /// Returns true if the vector is present in the dictionary and is going to be welded
            /// </summary>
            /// <param name="v"></param>
            /// <returns></returns>
            public bool Add(Vector3 v)
            {
                var int3 = ToInt3(v);
                var originalCount = VectorRemapping.Count;
                if (Lookup.ContainsKey(int3))
                {
                    VectorRemapping.Add(originalCount, Lookup[int3]);
                    return true;
                }
                var r = Lookup.Count;
                Lookup.Add(int3, r);
                Debug.Assert(Lookup[int3] == r);
                return false;
            }

            public Dictionary<int, int> VectorRemapping = new Dictionary<int, int>();
            public Dictionary<Int3, int> Lookup = new Dictionary<Int3, int>();
            public float Multiplier;

            public Int3 ToInt3(Vector3 v)
                => new Int3(ToRoundedInt(v.X), ToRoundedInt(v.Y), ToRoundedInt(v.Z));

            public int ToRoundedInt(float x)
                => (int)(x * Multiplier).Round();
        }

        public static IGeometry MergeWelded(this IArray<IGeometry> geometries, float weldingTolerance)
        {
            var triMeshes = geometries.Where(g => g != null && g.NumFaces > 0).Select(g => g.ToTriMesh()).ToArray();
            var newFaceCount = triMeshes.Sum(g => g.NumFaces);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var objectIds = new int[newFaceCount];
            var materialIds = new int[newFaceCount];
            var indices = new List<int>();
            var faceOffset = 0;

            // TODO: maybe make this an argument? A null or default welder could simply not weld.
            var welder = new WeldingVertexLookup(1 / weldingTolerance);

            // TODO: this assumes the presence of object ids and material ids
            foreach (var g in triMeshes)
            {
                Debug.Assert(g.Vertices.Count == g.UVs.Count);
                for (var i = 0; i < g.Vertices.Count; ++i)
                {
                    if (!welder.Add(g.Vertices[i]))
                    {
                        verts.Add(g.Vertices[i]);
                        uvs.Add(g.UVs[i]);
                    }
                }

                for (var i = 0; i < g.Indices.Count; ++i)
                {
                    var oldVtxIdx = g.Indices[i];
                    var newVtxIdx = welder.VectorRemapping[oldVtxIdx];
                    indices.Add(newVtxIdx);
                }

                g.MaterialIds()?.CopyTo(materialIds, faceOffset);
                g.ObjectIds()?.CopyTo(objectIds, faceOffset);
                faceOffset += g.NumFaces;
            }

            // TODO: maybe this could all be done using real arrays, probably be faster to serialize, etc.  But that is for later.
            return TriMesh(verts.ToIArray(), indices.ToIArray(), uvs.ToIArray(), materialIds.ToIArray(), objectIds.ToIArray());
        }

        // TODO: this function need to be generalized to handle all attributes correctly. In fact I think it should proably happen at the IG3D level.
        public static IGeometry MergeUnwelded(this IArray<IGeometry> geometries)
        {
            var triMeshes = geometries.Where(g => g != null && g.NumFaces > 0).Select(g => g.ToTriMesh()).ToArray();
            var newVertCount = triMeshes.Sum(g => g.Vertices.Count);
            var newFaceCount = triMeshes.Sum(g => g.NumFaces);
            var verts = new Vector3[newVertCount];
            var uvs = new Vector2[newVertCount];
            var objectIds = new int[newFaceCount];
            var materialIds = new int[newFaceCount];
            var indices = new List<int>();
            var vtxOffset = 0;
            var faceOffset = 0;

            // TODO: this assumes the presence of object ids and material ids
            foreach (var g in triMeshes)
            {
                g.Vertices.CopyTo(verts, vtxOffset);
                g.UVs.CopyTo(uvs, vtxOffset);
                g.Indices.Add(vtxOffset).AddTo(indices);
                g.MaterialIds()?.CopyTo(materialIds, faceOffset);
                g.ObjectIds()?.CopyTo(objectIds, faceOffset);
                vtxOffset += g.Vertices.Count;
                faceOffset += g.NumFaces;
            }

            // TODO: maybe this could all be done using real arrays, probably be faster to serialize, etc.  But that is for later.
            return TriMesh(verts.ToIArray(), indices.ToIArray(), uvs.ToIArray(), materialIds.ToIArray(), objectIds.ToIArray());
        }

        public static bool AreAllIndicesValid(this IGeometry self)
            => self.Indices.All(i => i.Between(0, self.Vertices.Count - 1));
        
        public static bool AreAllVerticesUsed(this IGeometry self)
        {
            var bools = new bool[self.Vertices.Count];
            foreach (var i in self.Indices.ToEnumerable())
                bools[i] = true;
            return bools.All(b => b);
        }

        public static bool IsValid(this IGeometry self)
            => self.AreAllIndicesValid();

        public static IArray<int> FaceIndicesToCornerIndices(this IGeometry g3d, IArray<int> faceIndices)
        {
            if (g3d.PointsPerFace > 0)
                return faceIndices.GroupIndicesToIndices(g3d.PointsPerFace);
            var r = new List<int>();
            var topo = g3d.Topology;
            for (var i = 0; i < faceIndices.Count; ++i)
            {
                var index = faceIndices[i];
                var faceSize = g3d.FaceSizes[index];
                var faceIndex = topo.FacesToCorners[index];
                for (var j=0; j < faceSize; ++j)
                    r.Add(g3d.Indices[faceIndex + j]);
            }

            return r.ToIArray();
        }

        public static IGeometry RemapFaces(this IGeometry g, IArray<int> faceRemap)
        {
            var cornerRemap = g.FaceIndicesToCornerIndices(faceRemap);
            return g.VertexAttributes()
                .Concat(g.NoneAttributes())
                .Concat(g.FaceAttributes().Select(attr => attr.Remap(faceRemap)))
                .Concat(g.EdgeAttributes().Select(attr => attr.Remap(cornerRemap)))
                .Concat(g.CornerAttributes().Select(attr => attr.Remap(cornerRemap)))
                .ToG3D()
                .ToIGeometry();
        }

        public static IGeometry CopyFaces(this IGeometry g, Func<int, bool> predicate)
            => g.RemapFaces(g.NumFaces.Select(i => i).IndicesWhere(predicate).ToIArray());

        public static IGeometry CopyFaces(this IGeometry g, int from, int count)
            => g.CopyFaces(i => i >= from && i < from + count);

        public static IArray<IGeometry> CopyFaceGroups(this IGeometry g, int size)
            => g.GetFaces().Count.DivideRoundUp(size).Select(i => CopyFaces(g, i * size, size));

        public static Face GetFace(this IGeometry g, int i)
            => new Face(g, i);

        public static IArray<Face> GetFaces(this IGeometry g) 
            => g.NumFaces.Select(g.GetFace);
        
        public static bool AreCoplanar(this IEnumerable<Face> faces, float tolerance = (float)Constants.OneTenthOfADegree)
            => faces.Select(f => f.Normal()).AreColinear(tolerance);

        public static bool AreColinear(this IEnumerable<Vector3> vectors, Vector3 reference, float tolerance = (float)Constants.OneTenthOfADegree)
            => !reference.IsNaN() && vectors.All(v => v.Colinear(reference, tolerance));

        public static bool AreColinear(this IEnumerable<Vector3> vectors, float tolerance = (float) Constants.OneTenthOfADegree)
            => vectors.ToList().AreColinear(tolerance);

        public static bool AreColinear(this IList<Vector3> vectors, float tolerance = (float)Constants.OneTenthOfADegree)
            => vectors.Count <= 1 || vectors.Skip(1).AreColinear(vectors[0], tolerance);

        public static IGeometry Merge(this IGeometry g, params IGeometry[] others)
        {
            var gs = others.ToList();
            gs.Insert(0, g);
            return gs.Merge();
        }

        public static IEnumerable<IAttribute> SortedAttributes(this IGeometry g)
            => g.Attributes.OrderBy(attr => attr.Descriptor.ToString());

        public static IGeometry ToIGeometry(this IEnumerable<IAttribute> attributes)
            => attributes.ToG3D().ToIGeometry();

        public static IGeometry ToIGeometry(this IG3D g)
            => new G3DAdapter(g);

        public static IG3D ToG3D(this IGeometry g)
            => g is IG3D g3d ? g3d : g.Attributes.ToG3D();

        public static bool Planar(this IGeometry g, float tolerance = Constants.Tolerance)
        {
            if (g.NumFaces <= 1) return true;
            var normal = g.GetFace(0).Normal();
            return g.GetFaces().All(f => f.Normal().AlmostEquals(normal, tolerance));
        }

        public static bool AreTrianglesRepeated(this IGeometry g)
            => g.GetFaces().CountUnique() != g.NumFaces;

        public static bool HasDegenerateFaceIndices(this IGeometry g)
            => g.GetFaces().Any(f => f.HasDegenerateIndices());

        public static void Validate(this IGeometry g3d)
        {
            (g3d as IG3D).Validate();

            if (g3d.FaceSizes.Count != g3d.NumFaces)
                throw new Exception("Expected the face sizes array to be equal to the number of faces");
            if (!g3d.AreAllIndicesValid())
                throw new Exception("Not all indices are valid");

            var topo = g3d.Topology;
            var faceIndex = 0;
            for (var i = 0; i < g3d.NumFaces; ++i)
            {
                if (faceIndex != topo.FacesToCorners[i])
                    throw new Exception("Topology face indices is incorrect");
                var faceSize = g3d.FaceSizes[i];
                
                var face = g3d.GetFace(i);
                if (face.Count != faceSize)
                    throw new Exception("Face does not have correct size");

                for (var j = 0; j < faceSize; ++j)
                {
                    var index = g3d.Indices[faceIndex + j];
                    if (index < 0 || index >= g3d.Vertices.Count)
                        throw new Exception("Vertex index out of range");

                    if (index != face[j])
                        throw new Exception("Face does not have valid index");
                }

                faceIndex += faceSize;
            }
        }

        /// <summary>
        /// Creates a revolved face ... note that the last points are on top of the original 
        /// </summary>
        public static IGeometry RevolveAroundAxis(this IArray<Vector3> points, Vector3 axis, int segments = 4)
        {
            var verts = new List<Vector3>();
            for (var i = 0; i < segments; ++i)
            {
                var angle = Constants.TwoPi / segments;
                points.Rotate(axis, angle).AddTo(verts);
            }

            return QuadMesh(verts.ToIArray(), ComputeQuadMeshStripIndices(segments-1, points.Count-1));
        }

        public static IGeometry RemoveUnusedVertices(this IGeometry g)
        {
            var tmp = false.Repeat(g.Vertices.Count).ToArray();
            for (var i=0; i < g.Indices.Count; ++i)
                tmp[g.Indices[i]] = true;

            var n = 0;
            var remap = new int[g.Vertices.Count];
            var newVertices = new List<Vector3>();
            for (var i = 0; i < remap.Length; ++i)
            {
                if (tmp[i])
                {
                    remap[i] = n++;                    
                    newVertices.Add(g.Vertices[i]);
                }
                else
                    remap[i] = -1;
            }

            // Just make sure that everything makes sense
            for (var i = 0; i < g.Indices.Count; ++i)
            {
                var index = g.Indices[i];
                Debug.Assert(tmp[index] = true);
                var newIndex = remap[index];
                Debug.Assert(newIndex >= 0);
                var vtx = g.Vertices[index];
                var vtx2 = newVertices[newIndex];
                Debug.Assert(vtx == vtx2);
            }

            // Set up the new indices
            var newIndices = new List<int>();
            for (var i = 0; i < g.Indices.Count; ++i)
            {
                newIndices.Add(remap[g.Indices[i]]);
            }

            return Mesh(g.PointsPerFace, newVertices.ToIArray(), newIndices.ToIArray());
        }

        public static bool IsEqual(this IGeometry g1, IGeometry g2)
            => g1.NumFaces == g2.NumFaces
               && g1.PointsPerFace == g2.PointsPerFace
               && g1.Indices.SequenceEquals(g2.Indices)
               && g1.Vertices.SequenceEquals(g2.Vertices)
               && g1.UVs.SequenceEquals(g2.UVs)
               && g1.FaceSizes.SequenceEquals(g2.FaceSizes);

        /// <summary>
        /// Creates a TriMesh from four points. 
        /// </summary>
        public static IGeometry TriMeshFromQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            => TriMesh(new[] {a, b, c, c, d, a}.ToIArray());

        public static IGeometry SetMaterialIds(this IGeometry g, int id)
            => g.SetMaterialIds(id.Repeat(g.NumFaces));

        public static IGeometry SetMaterialIds(this IGeometry g, IArray<int> ids)
            => g.ReplaceAttribute(ids.ToMaterialIdsAttribute()).ToIGeometry();

        public static IGeometry SetObjectIds(this IGeometry g, int id)
            => g.SetObjectIds(id.Repeat(g.NumFaces));

        public static IGeometry SetObjectIds(this IGeometry g, IArray<int> ids)
            => g.ReplaceAttribute(ids.ToObjectIdsAttribute()).ToIGeometry();

        public static bool SequenceAlmostEquals(this IArray<Vector3> vs1, IArray<Vector3> vs2, float tolerance = Constants.Tolerance)
            => vs1.Count == vs2.Count && vs1.Indices().All(i => vs1[i].AlmostEquals(vs2[i], tolerance));

        public static bool SameVerticesAndTopology(this IGeometry g1, IGeometry g2, float tolerance = Constants.Tolerance)
            => g1.Indices.SequenceEquals(g2.Indices) && g1.Vertices.SequenceAlmostEquals(g2.Vertices, tolerance);

        public static Vector3 CenterPoint(this IGeometry g)
            => g.Vertices.Average();

        public static IGeometry SimplePolygonTessellate(this IEnumerable<Vector3> points)
        {
            var pts = points.ToList();
            var cnt = pts.Count;
            var sum = Vector3.Zero;
            var idxs = new List<int>(pts.Count * 3);
            for (var i = 0; i < pts.Count; ++i)
            {
                idxs.Add(i);
                idxs.Add(i + 1 % cnt);
                idxs.Add(cnt);
                sum += pts[i];
            }

            var midPoint = sum / pts.Count;
            pts.Add(midPoint);

            return Geometry.TriMesh(pts.ToIArray(), idxs.ToIArray());
        }

        public static bool IsCoplanar(this IGeometry g, float tolerance = (float)Constants.OneTenthOfADegree) 
            => g.GetFaces().ToEnumerable().AreCoplanar(tolerance);

    }
}
