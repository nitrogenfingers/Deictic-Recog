using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Microsoft.Xna.Framework.Graphics {
    public static class Conversions {
        public static Vector3 ToVector3(this byte[] data, int start) {
            return new Vector3(
                BitConverter.ToSingle(data, start),
                BitConverter.ToSingle(data, start + 4),
                BitConverter.ToSingle(data, start + 8)
            );
        }
        public static Vector2 ToVector2(this byte[] data, int start) {
            return new Vector2(
                BitConverter.ToSingle(data, start),
                BitConverter.ToSingle(data, start + 4)
            );
        }
        public static Color ToColor(this byte[] data, int start) {
            return new Color(data[start], data[start + 1], data[start + 2], data[start + 3]);
        }
    }
    public class MeshDataContainer {
        public VertexPositionColorTexture[] Vertices;
        public short[] Indices;
    }

    public static class ModelMeshExtension {
        public static MeshDataContainer GetMeshData(this ModelMesh mesh) {
            if (mesh == null) return null;
            if (mesh.Tag == null) {
                mesh.Tag = new MeshDataContainer();
            }
            return mesh.Tag as MeshDataContainer;
        }

        public static VertexPositionColorTexture[] GetVerts(this ModelMesh mesh) {
            MeshDataContainer data = mesh.GetMeshData();
            if (data == null) return null;
            if (data.Vertices == null) {
                IEnumerable<VertexPositionColorTexture> i_verts = null;
                for (int p = 0; p < mesh.MeshParts.Count; ++p) {
                    if (i_verts == null) i_verts = mesh.MeshParts[p].GetVerts();
                    else i_verts = i_verts.Concat(mesh.MeshParts[p].GetVerts());
                }
                data.Vertices = i_verts.ToArray();
            }
            return data.Vertices;
        }
        public static short[] GetInds(this ModelMesh mesh) {
            MeshDataContainer data = mesh.GetMeshData();
            if (data == null) return null;
            if (data.Indices == null) {
                IEnumerable<short> i_verts = null;
                for (int p = 0; p < mesh.MeshParts.Count; ++p) {
                    if (i_verts == null) i_verts = mesh.MeshParts[p].GetInds();
                    else i_verts = i_verts.Concat(mesh.MeshParts[p].GetInds());
                }
                data.Indices = i_verts.ToArray();
            }
            return data.Indices;
        }
    }
    public static class ModelMeshPartExtension {

        public static MeshDataContainer GetPartData(this ModelMeshPart Part) {
            if (Part == null) return null;
            if (Part.Tag == null) {
                Part.Tag = new MeshDataContainer();
            }
            return Part.Tag as MeshDataContainer;
        }

        public static VertexPositionColorTexture[] GetVerts(this ModelMeshPart Part) {
            MeshDataContainer data = Part.GetPartData();
            if (data == null) return null;
            if (data.Vertices == null) {
                VertexBuffer vb = Part.VertexBuffer;
                VertexPositionColorTexture[] verts = new VertexPositionColorTexture[Part.NumVertices];
                byte[] vb_verts = vb.Tag as byte[];
                if (vb_verts == null) {
                    vb_verts = new byte[vb.VertexCount * vb.VertexDeclaration.VertexStride];
                    vb.GetData<byte>(vb_verts);
                    vb.Tag = vb_verts;
                }
                int offset = Part.VertexOffset;
                int vertStride = vb.VertexDeclaration.VertexStride;
                var elements = vb.VertexDeclaration.GetVertexElements();
                for (int v = 0; v < verts.Length; ++v) {
                    verts[v].Color = Color.White;
                }
                foreach (var element in elements) {
                    switch (element.VertexElementUsage) {
                        case VertexElementUsage.Position: {
                            for (int v = 0; v < verts.Length; ++v) {
                                verts[v].Position = vb_verts.ToVector3((v + offset) * vertStride + element.Offset);
                            }
                        } break;
                        /*case VertexElementUsage.Normal: {
                            for (int v = 0; v < verts.Length; ++v) {
                                verts[v].Normal = vb_verts.ToVector3((v + offset) * vertStride + element.Offset);
                            }
                        } break;*/
                        case VertexElementUsage.TextureCoordinate: {
                            for (int v = 0; v < verts.Length; ++v) {
                                verts[v].TextureCoordinate = vb_verts.ToVector2((v + offset) * vertStride + element.Offset);
                            }
                        } break;
                        case VertexElementUsage.Color: {
                            for (int v = 0; v < verts.Length; ++v) {
                                verts[v].Color = vb_verts.ToColor((v + offset) * vertStride + element.Offset);
                            }
                        } break;
                    }
                }
                /*for (int v = 0; v < verts.Length; ++v) {
                    verts[v].Position = vb_verts[v + offset].Position;
                    verts[v].TextureCoordinate = vb_verts[v + offset].TextureCoordinate;
                    verts[v].Normal = vb_verts[v + offset].Normal;
                    verts[v].Color = Color.White;
                }*/
                data.Vertices = verts;
            }
            return data.Vertices;
        }
        public static short[] GetInds(this ModelMeshPart Part) {
            MeshDataContainer data = Part.GetPartData();
            if (data == null) return null;
            if (data.Indices == null) {
                IndexBuffer ib = Part.IndexBuffer;
                short[] inds = new short[Part.PrimitiveCount * 3];
                short[] ib_inds = ib.Tag as short[];
                if (ib_inds == null) {
                    ib_inds = new short[ib.IndexCount];
                    ib.GetData<short>(ib_inds);
                    ib.Tag = ib_inds;
                }
                int offset = Part.StartIndex;
                for (int i = 0; i < inds.Length; ++i) {
                    inds[i] = ib_inds[i + offset];
                }
                data.Indices = inds;
            }
            return data.Indices;
        }

    }
}
