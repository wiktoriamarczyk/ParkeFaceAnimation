using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FaceAnimation : MonoBehaviour
{
    [SerializeField] BoxCollider collider;
    [SerializeField] Camera camera;
    [SerializeField] MeshFilter mesh;
    [SerializeField] MeshCollider srcMesh;
    [SerializeField] Text recText;
    [SerializeField] int resolutionX = 256;
    [SerializeField] int resolutionY = 256;
    [SerializeField] float CapturesPerSecond = 4;
    [SerializeField] eInterpolationNode interpolationNode = eInterpolationNode.Linear;

    Quad quad;
    Animation animation;
    List<Point> animPoints = new List<Point>();

    eMode mode = eMode.Playing;
    int pointCount = 0;
    float timeLeftToCapture = 0;

    enum eInterpolationNode
    {
        Linear = 0,
        Parke = 1
    }

    enum eMode
    {
        Playing,
        Recording
    };

    class Quad
    {
        public Vector3 GetPointOnQuad(float u, float v)
        {
            return points[0] + (points[1] - points[0]) * u + (points[2] - points[0]) * v;
        }

        public static Quad FromCollider(BoxCollider b)
        {
            Quad quad = new Quad();
            quad.points[0] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y,  0) * 0.5f);
            quad.points[1] = b.transform.TransformPoint(b.center + new Vector3( b.size.x, -b.size.y,  0) * 0.5f);
            quad.points[2] = b.transform.TransformPoint(b.center + new Vector3(-b.size.x,  b.size.y,  0) * 0.5f);
            return quad;
        }

        public Vector3[] points = new Vector3[3];
    }

    struct Point
    {
        public Vector3 baric;
        public int triID;
        public int index;

        public Vector3 GetLocalPos(int[] triangles, Vector3[] vertices)
        {
            Vector3 n0 = vertices[triangles[triID * 3 + 0]];
            Vector3 n1 = vertices[triangles[triID * 3 + 1]];
            Vector3 n2 = vertices[triangles[triID * 3 + 2]];

            return n0 * baric.x + n1 * baric.y + n2 * baric.z;
        }
    };

    struct Frame
    {
        public Vector3[] points;
        public float time;
    };

    class Animation
    {
        public Animation(FaceAnimation owner)
        {
            this.owner = owner;
            startTime = Time.time;
        }

        public List<Frame> frames = new List<Frame>();
        public FaceAnimation owner;
        public float duration = 0;
        float time = 0;
        float startTime = 0;
        int frame = 0;

        public void CaptureAnimFrame()
        {
            Frame frame = new Frame();
            frame.points = new Vector3[owner.pointCount];
            frame.time = Time.time - startTime;

            Mesh sharedMesh = owner.srcMesh.sharedMesh;
            var meshTriangles = sharedMesh.triangles;
            var meshVertices = sharedMesh.vertices;

            for (int i=0; i < owner.animPoints.Count; ++i)
                frame.points[i] = owner.animPoints[i].GetLocalPos(meshTriangles,meshVertices);

            frames.Add(frame);
            duration = frame.time;
        }

        public void Update(float dt, Mesh mesh)
        {
            time += dt;

            if (time > duration)
            {
                time = 0;
                frame = 0;
            }
            else while(frame + 1 < frames.Count && time >= frames[frame+1].time)
            {
                frame++;
            }

            if (frame + 1 < frames.Count)
                ParkeInterpolate(time, frames[frame], frames[frame + 1], mesh);
        }

        void ParkeInterpolate(float time, Frame previous, Frame next, Mesh mesh)
        {
            Vector3[] vertices = new Vector3[previous.points.Length];
            float phaseFraction = (time - previous.time) / (next.time - previous.time);
            double phi = phaseFraction * Math.PI;
            float C;

            if (owner.interpolationNode == eInterpolationNode.Parke)
                C = (float)((1.0 - Math.Cos(phi)) * 0.5);
            else
                C = phaseFraction;

            for (int i = 0; i < previous.points.Length; i++)
            {
                Vector3 difference = next.points[i] - previous.points[i];
                Vector3 currentPosition = previous.points[i] + C*difference;
                vertices[i] = currentPosition;
            }
            mesh.vertices = vertices;
        }
    }

    void Start()
    {
        Point[,] animPointsMatrix = new Point[resolutionX, resolutionY];
        quad = Quad.FromCollider(collider);
        pointCount = 0;

        List<Vector2> uvs = new List<Vector2>();

        for (int y = 0; y < resolutionY; y++ )
        {
            for (int x = 0; x < resolutionX; x++)
            {
                float u = (float)x / (resolutionX - 1);
                float v = (float)y / (resolutionY - 1);
                Vector3 point = quad.GetPointOnQuad(u, v);
                // ray from camera position to "point"
                var dir = point - camera.transform.position;
                Ray ray = new Ray(camera.transform.position, dir);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit,dir.magnitude*2f) && hit.collider != collider)
                {
                    animPointsMatrix[y,x].triID = hit.triangleIndex;
                    animPointsMatrix[y,x].baric = hit.barycentricCoordinate;
                    animPointsMatrix[y,x].index = pointCount++;

                    uvs.Add(hit.textureCoord);
                    animPoints.Add(animPointsMatrix[y,x]);

                    Debug.DrawLine(hit.point, hit.point+(Vector3.up*0.001f), Color.red, 60);
                }
                else
                {
                    animPointsMatrix[y,x].triID = -1;
                    animPointsMatrix[y,x].index = -1;
                }
            }
        }

        // construct mesh indices
        List<int> indices = new List<int>();

        for (int y = 1; y < resolutionY; y++)
        {
            for (int x = 1; x < resolutionX; x++)
            {
                if (animPointsMatrix[y-1,x-1].triID >= 0 &&
                    animPointsMatrix[y-1,x  ].triID >= 0 &&
                    animPointsMatrix[y  ,x-1].triID >= 0)
                {
                    indices.Add( animPointsMatrix[y  ,x-1].index);
                    indices.Add( animPointsMatrix[y-1,x  ].index);
                    indices.Add( animPointsMatrix[y-1,x-1].index);
                }

                if (animPointsMatrix[y  ,x  ].triID >= 0 &&
                    animPointsMatrix[y-1,x  ].triID >= 0 &&
                    animPointsMatrix[y  ,x-1].triID >= 0)
                {
                    indices.Add( animPointsMatrix[y  ,x  ].index);
                    indices.Add( animPointsMatrix[y-1,x  ].index);
                    indices.Add( animPointsMatrix[y  ,x-1].index);
                }
            }
        }

        // construct mesh vertices
        Mesh sharedMesh = srcMesh.sharedMesh;
        var meshTriangles = sharedMesh.triangles;
        var meshVertices = sharedMesh.vertices;

        Vector3[] vertices = new Vector3[animPoints.Count];
        for (int i = 0; i < animPoints.Count; ++i)
            vertices[i] = animPoints[i].GetLocalPos(meshTriangles,meshVertices);

        // create mesh
        mesh.mesh.vertices = vertices;
        mesh.mesh.uv = uvs.ToArray();
        mesh.mesh.SetIndices(indices.ToArray(),MeshTopology.Triangles,0);
    }

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Return))
        {
            if (mode == eMode.Playing)
            {
                animation = new Animation(this);
                animation.owner = this;
                mode = eMode.Recording;
                recText.enabled = true;
            }
            else if (mode== eMode.Recording)
            {
                mode = eMode.Playing;
                recText.enabled = false;
            }
        }

        if (mode == eMode.Recording)
        {
            timeLeftToCapture -= Time.deltaTime;
            if (timeLeftToCapture <= 0)
            {
                timeLeftToCapture = 1.0f / CapturesPerSecond;
                animation.CaptureAnimFrame();
            }
        }

        if (animation != null && mode == eMode.Playing)
            animation.Update(Time.deltaTime,mesh.mesh);
    }
}
