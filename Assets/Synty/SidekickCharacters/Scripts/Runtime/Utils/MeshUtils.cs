// Copyright (c) 2024 Synty Studios Limited. All rights reserved.
//
// Use of this software is subject to the terms and conditions of the Synty Studios End User Licence Agreement (EULA)
// available at: https://syntystore.com/pages/end-user-licence-agreement
//
// For additional details, see the LICENSE.MD file bundled with this software.

using UnityEngine;

namespace Synty.SidekickCharacters.Utils
{
    /// <summary>
    ///     A collection of utility methods related to operations on Meshes.
    /// </summary>
    public class MeshUtils
    {
        /// <summary>
        ///     Creates and returns a copy of the passed in Mesh.
        /// </summary>
        /// <param name="mesh">The mesh to copy.</param>
        /// <returns>A copy of the passed in Mesh.</returns>
        public static Mesh CopyMesh(Mesh mesh)
        {
            Mesh newMesh = new Mesh
            {
                name = mesh.name,
                vertices = mesh.vertices,
                triangles = mesh.triangles,
                uv = mesh.uv,
                normals = mesh.normals,
                colors = mesh.colors,
                tangents = mesh.tangents,
                boneWeights = mesh.boneWeights,
                bindposes = mesh.bindposes
            };
            return newMesh;
        }
    }
}
