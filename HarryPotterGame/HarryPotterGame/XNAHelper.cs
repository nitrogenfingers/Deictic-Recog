using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace HarryPotterGame {
    public static class XNAHelper {

        public static Point ToPoint(this Vector2 vec) {
            return new Point((int)Math.Round(vec.X), (int)Math.Round(vec.Y));
        }
         
        private class ModelCacheStuff {
            public Matrix[] BoneCache;
        }

        public static void Draw(this BoundingSphere sphere, Model sphererep, Matrix view, Matrix proj) {
            Matrix world = Matrix.CreateScale(sphere.Radius) * Matrix.CreateTranslation(sphere.Center);

            Matrix[] bonetransforms = new Matrix[sphererep.Bones.Count];
            sphererep.CopyAbsoluteBoneTransformsTo(bonetransforms);

            foreach (ModelMesh mesh in sphererep.Meshes) {
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.View = view;
                    effect.Projection = proj;
                    effect.World = world;

                    effect.TextureEnabled = false;

                }
                mesh.Draw();
            }
        }

        public static void Draw(this VertexPositionColorTexture[] vlist, short vcount, short[] ilist, short icount, Matrix world, Matrix view, Matrix proj, BasicEffect effect) {
            if (icount <= 0) return;
            GraphicsDevice device = effect.GraphicsDevice;
            if (vlist.Length == 0 || ilist.Length == 0) return;

            effect.View = view;
            effect.Projection = proj;
            effect.World = world;

            for (int p = 0; p < effect.CurrentTechnique.Passes.Count; ++p) {
                effect.CurrentTechnique.Passes[p].Apply();
                device.DrawUserIndexedPrimitives<VertexPositionColorTexture>(PrimitiveType.TriangleList, vlist, 0, vcount, ilist, 0, icount / 3);
            }
        }

        public static void Draw(this ModelMesh mesh, Matrix world, Matrix view, Matrix proj, Vector3 diffuse) {
            mesh.Draw(world, view, proj, new Vector4(diffuse, 1));
        }
        public static void Draw(this ModelMesh mesh, Matrix world, Matrix view, Matrix proj, Vector4 diffuse) {
            foreach (BasicEffect effect in mesh.Effects) {
                effect.View = view;
                effect.Projection = proj;
                effect.World = world;
                effect.LightingEnabled = false;

                effect.DiffuseColor = new Vector3(diffuse.X, diffuse.Y, diffuse.Z);
                effect.Alpha = diffuse.W;
            }
            mesh.Draw();
        }

        public static void Draw(this Model model, Matrix world, Matrix view, Matrix proj, Vector3 diffuse) {
            model.Draw(world, view, proj, new Vector4(diffuse, 1));
        }
        public static void Draw(this Model model, Matrix world, Matrix view, Matrix proj, Vector4 diffuse) {
            ModelCacheStuff cache = model.Tag as ModelCacheStuff;
            if (cache == null) {
                cache = new ModelCacheStuff() {
                    BoneCache = new Matrix[model.Bones.Count],
                };
                model.CopyAbsoluteBoneTransformsTo(cache.BoneCache);
                model.Tag = cache;
            }
            foreach (ModelMesh mesh in model.Meshes) {
                mesh.Draw(cache.BoneCache[mesh.ParentBone.Index] * world, view, proj, diffuse);
            }
        }

        static float? RayIntersectsTriangle(ref Ray ray,
                                  ref Vector3 vertex1,
                                  ref Vector3 vertex2,
                                  ref Vector3 vertex3) {
            // Compute vectors along two edges of the triangle.
            Vector3 edge1, edge2;

            Vector3.Subtract(ref vertex2, ref vertex1, out edge1);
            Vector3.Subtract(ref vertex3, ref vertex1, out edge2);

            // Compute the determinant.
            Vector3 directionCrossEdge2;
            Vector3.Cross(ref ray.Direction, ref edge2, out directionCrossEdge2);

            float determinant;
            //Vector3.Dot(ref edge1, ref directionCrossEdge2, out determinant);
            determinant = edge1.X * directionCrossEdge2.X + edge1.Y * directionCrossEdge2.Y + edge1.Z * directionCrossEdge2.Z;

            // Cull backfacing
            if (determinant > 0) { return null; }

            // If the ray is parallel to the triangle plane, there is no collision.
            if (determinant > -float.Epsilon/* && determinant < float.Epsilon*/) { return null; }

            // Calculate the U parameter of the intersection point.
            Vector3 distanceVector;
            Vector3.Subtract(ref ray.Position, ref vertex1, out distanceVector);

            float triangleU;
            Vector3.Dot(ref distanceVector, ref directionCrossEdge2, out triangleU);

            // Make sure it is inside the triangle.
            if (triangleU > 0 || triangleU < determinant) { return null; }

            // Calculate the V parameter of the intersection point.
            Vector3 distanceCrossEdge1;
            Vector3.Cross(ref distanceVector, ref edge1, out distanceCrossEdge1);

            float triangleV;
            Vector3.Dot(ref ray.Direction, ref distanceCrossEdge1, out triangleV);

            // Make sure it is inside the triangle.
            if (triangleV > 0 || triangleU + triangleV < determinant) { return null; }

            // Compute the distance along the ray to the triangle.
            float rayDistance;
            Vector3.Dot(ref edge2, ref distanceCrossEdge1, out rayDistance);
            rayDistance /= determinant;

            // Is the triangle behind the ray origin?
            if (rayDistance < 0) { return null; }

            return rayDistance;
        }
        // Assumes the line is from pointing upward from (0, 0)
        static float? RayIntersectsLine(Vector2 lineS, Vector2 lineE) {
            if ((lineS.X < 0) != (lineE.X < 0)) {
                float d = -lineS.X / (lineE.X - lineS.X);
                return MathHelper.Lerp(lineS.Y, lineE.Y, d);
            }
            return null;
        }


        static float? RayIntersectsTriangle(ref Ray ray,
                                  ref Vector3 vertex1,
                                  ref Vector3 vertex2,
                                  ref Vector3 vertex3, Vector3 axis)
        {
            Vector3 relV1 = vertex1 - ray.Position;
            Vector3 relV2 = vertex2 - ray.Position;
            Vector3 relV3 = vertex3 - ray.Position;
            Vector3 cross = Vector3.Cross(ray.Direction, axis);
            Vector2 v1 = new Vector2(Vector3.Dot(cross, relV1), Vector3.Dot(ray.Direction, relV1));
            Vector2 v2 = new Vector2(Vector3.Dot(cross, relV2), Vector3.Dot(ray.Direction, relV2));
            Vector2 v3 = new Vector2(Vector3.Dot(cross, relV3), Vector3.Dot(ray.Direction, relV3));
            float? l1d = RayIntersectsLine(v1, v2);
            float? l2d = RayIntersectsLine(v2, v3);
            float? l3d = RayIntersectsLine(v3, v1);
            float? res = l1d;
            if (l2d != null && (l2d ?? float.MaxValue) < (res ?? float.MaxValue)) res = l2d;
            if (l3d != null && (l3d ?? float.MaxValue) < (res ?? float.MaxValue)) res = l3d;
            return res;
        }

        public static float? Intersects(this Ray ray, ModelMesh mesh, Matrix meshTransform, Vector3 axis) {
            Matrix invMeshTrans = Matrix.Invert(meshTransform);
            Vector3 locDir = Vector3.TransformNormal(ray.Direction, invMeshTrans);
            float locDirL = locDir.Length();
            Ray locRay = new Ray(
                Vector3.Transform(ray.Position, invMeshTrans),
                locDir / locDirL
            );
            float? hit = locRay.Intersects(mesh, Vector3.TransformNormal(axis, invMeshTrans));
            if (hit == null) return null;
            return hit.Value / locDirL;
            /*Vector3 hitPos = ray.Position + ray.Direction * hit.Value;
            return Vector3.Dot(ray.Direction, hitPos - ray.Position) / ray.Direction.LengthSquared();*/
        }
        public static float? Intersects(this Ray ray, ModelMesh mesh, Vector3 axis) {
            var verts = mesh.GetVerts();
            var inds = mesh.GetInds();
            float? maxDist = null;
            for (int i = 0; i < inds.Length; i += 3) {
                float? result = RayIntersectsTriangle(ref ray,
                    ref verts[inds[i + 0]].Position, ref verts[inds[i + 1]].Position, ref verts[inds[i + 2]].Position,
                    axis);
                if((result ?? float.MaxValue) < (maxDist ?? float.MaxValue)) {
                    maxDist = result;
                }
            }
            return maxDist;
        }


        //Non-axially locked version
        public static float? Intersects(this Ray ray, ModelMesh mesh, Matrix meshTransform)
        {
            Matrix invMeshTrans = Matrix.Invert(meshTransform);
            Vector3 locDir = Vector3.TransformNormal(ray.Direction, invMeshTrans);
            float locDirL = locDir.Length();
            Ray locRay = new Ray(
                Vector3.Transform(ray.Position, invMeshTrans),
                locDir / locDirL
            );
            float? hit = locRay.Intersects(mesh);
            if (hit == null) return null;
            return hit.Value / locDirL;
            /*Vector3 hitPos = ray.Position + ray.Direction * hit.Value;
            return Vector3.Dot(ray.Direction, hitPos - ray.Position) / ray.Direction.LengthSquared();*/
        }
        public static float? Intersects(this Ray ray, ModelMesh mesh)
        {
            var verts = mesh.GetVerts();
            var inds = mesh.GetInds();
            float? maxDist = null;
            for (int i = 0; i < inds.Length; i += 3)
            {
                float? result = RayIntersectsTriangle(ref ray,
                    ref verts[inds[i + 0]].Position, ref verts[inds[i + 1]].Position, ref verts[inds[i + 2]].Position);
                if ((result ?? float.MaxValue) < (maxDist ?? float.MaxValue))
                {
                    maxDist = result;
                }
            }
            return maxDist;
        }

        // From the XNA Tutorial "Selecting with Mouse"
        //
        // Microsoft XNA Community Game Platform
        // Copyright (C) Microsoft Corporation. All rights reserved.
        public static Ray CalculateCursorRay(this Vector2 cursorPosition, Matrix projectionMatrix, Matrix viewMatrix, GraphicsDevice device)
        {
            // create 2 positions in screenspace using the cursor position. 0 is as
            // close as possible to the camera, 1 is as far away as possible.
            Vector3 nearSource = new Vector3(cursorPosition, 0f);
            Vector3 farSource = new Vector3(cursorPosition, 1f);

            // use Viewport.Unproject to tell what those two screen space positions
            // would be in world space. we'll need the projection matrix and view
            // matrix, which we have saved as member variables. We also need a world
            // matrix, which can just be identity.
            Vector3 nearPoint = device.Viewport.Unproject(nearSource,
                projectionMatrix, viewMatrix, Matrix.Identity);

            Vector3 farPoint = device.Viewport.Unproject(farSource,
                projectionMatrix, viewMatrix, Matrix.Identity);

            // find the direction vector that goes from the nearPoint to the farPoint
            // and normalize it....
            Vector3 direction = farPoint - nearPoint;
            direction.Normalize();

            // and then create a new ray using nearPoint as the source.
            return new Ray(nearPoint, direction);
        }
    }
}
