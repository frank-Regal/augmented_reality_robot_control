using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicsInstancing : MonoBehaviour
{
    public GameObject MeshToCopy;

    private MeshRenderer meshRenderer;
    private Mesh mesh;
    private void Start()
    {
        meshRenderer = MeshToCopy.GetComponent<MeshRenderer>();
        mesh = MeshToCopy.GetComponent<Mesh>();
    }

    Matrix4x4[] offset_matrix = new Matrix4x4[] { Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity,
    Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity,
    Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity,
    Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity,
    Matrix4x4.identity, Matrix4x4.identity, Matrix4x4.identity};

    // Update is called once per frame
    void Update()
    {
        for (int j = 0; j < offset_matrix.Length; j++)
        {
            offset_matrix[j] = MeshToCopy.transform.localToWorldMatrix;

            for (int i = 0; i < 3; i++)
            {
                offset_matrix[j][i, 3] = offset_matrix[j][i, 3] + ((j + 1f) * 0.001f);
            }
        }

        Graphics.DrawMeshInstanced(mesh, 0, meshRenderer.material, offset_matrix, offset_matrix.Length);
    }
}
