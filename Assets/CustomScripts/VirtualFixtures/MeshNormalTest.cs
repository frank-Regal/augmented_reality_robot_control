using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshNormalTest : MonoBehaviour
{
    public GameObject MeshObject;
    public GameObject NormalVectorObject;
    public GameObject TempObject;

    Mesh mesh;
    Vector3 newpos = new Vector3(0, 0, 0);
    Matrix4x4 T_vertice;

    float angle;
    Vector3 axis;
    Vector3[] vertices;
    Quaternion mesh_rot;
    Vector3[] normals;
    Matrix4x4 Tow;

    float dist = 0;
    float first_dist = float.PositiveInfinity;
    float sec_dist = float.PositiveInfinity;

    Vector3 point_one = new Vector3(0, 0, 0);
    Vector3 point_two = new Vector3(0, 0, 0);

    public void GetTwoClosestVertices(ref Vector3[] vertices, 
        ref Vector3 closest_point, 
        ref Vector3 out_vec_one, 
        ref Vector3 out_vec_two)
    {
        for (int i = 0; i < vertices.Length; i++)
        {
            dist = (vertices[i] - closest_point).magnitude;
            if (dist < first_dist)
            {
                out_vec_one = vertices[i];
                first_dist = dist;
            } 
            else if (dist < sec_dist)
            {
                out_vec_two = vertices[i];
                sec_dist = dist;
            }
        }  
    }


    // Start is called before the first frame update
    void Start()
    {
        // obtain the normals from the Mesh
        mesh = MeshObject.GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        mesh_rot = MeshObject.GetComponent<Transform>().rotation;
        normals = mesh.normals;
        Tow = MeshObject.GetComponent<Transform>().transform.localToWorldMatrix;
        
        // Fill Rotation Matrix with Object Rotation
        for (int k = 0; k < 3; k++){
            for (int w = 0; w < 3; w++)
            {
                T_vertice[k, w] = Tow[k, w];
            }
        }
        
        for (int i = 0; i < normals.Length; i++)
        {
            // Rotate and Translate each vertex of the mesh of the object
            vertices[i] = Tow.MultiplyPoint3x4(vertices[i]);

            // Add the translational component of the vertex of the mesh to the transformation matrix
            for (int j = 0; j < 3; j++)
            {
                T_vertice[j, 3] = vertices[i][j];
            }

            // Rotate and Translate the normal vectors founds at each vertex
            normals[i] = T_vertice.MultiplyPoint3x4(normals[i].normalized);

            // Create new objects
            Instantiate(NormalVectorObject, vertices[i], mesh_rot);
            Instantiate(NormalVectorObject, normals[i], mesh_rot);
            
        }

        // assign the array of normals to the mesh
        //mesh.normals = normals;



    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
