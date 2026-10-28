using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Guadalupe.Editor.Environment
{
    /// <summary>
    /// Construye mallas procedurales por submallas (una por material) a partir de primitivas simples:
    /// paredes extruidas desde una planta, tapas, cajas orientadas y caras planas convexas.
    /// </summary>
    /// <remarks>
    /// Las coordenadas UV se calculan en metros de mundo multiplicados por <see cref="UvScale"/>,
    /// de modo que la escala de las texturas es la misma en todos los edificios.
    /// </remarks>
    public sealed class MeshBuilder
    {
        /// <summary>Repeticiones de textura por metro (1 unidad UV = 4 m).</summary>
        public const float UvScale = 0.25f;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<List<int>> _submeshes = new List<List<int>>();

        public MeshBuilder(int submeshCount)
        {
            for (int i = 0; i < submeshCount; i++)
            {
                _submeshes.Add(new List<int>());
            }
        }

        /// <summary>Indica si no se ha añadido ninguna geometría.</summary>
        public bool IsEmpty => _vertices.Count == 0;

        /// <summary>
        /// Añade un prisma vertical: paredes desde <paramref name="baseY"/> hasta <paramref name="topY"/>
        /// y tapa superior plana. La base no se cierra porque queda enterrada en el terreno.
        /// </summary>
        /// <param name="footprint">Planta en sentido antihorario (X este, Y norte).</param>
        public void AddExtrusion(IReadOnlyList<Vector2> footprint, float baseY, float topY, int wallSubmesh, int topSubmesh)
        {
            AddWalls(footprint, baseY, topY, wallSubmesh);
            AddCap(footprint, topY, topSubmesh, facingUp: true);
        }

        /// <summary>Añade solo las paredes de un prisma vertical, con la normal hacia fuera.</summary>
        public void AddWalls(IReadOnlyList<Vector2> footprint, float baseY, float topY, int submesh)
        {
            float perimeter = 0f;
            for (int i = 0; i < footprint.Count; i++)
            {
                Vector2 a2 = footprint[i];
                Vector2 b2 = footprint[(i + 1) % footprint.Count];
                float length = Vector2.Distance(a2, b2);

                // Con el anillo antihorario, la normal exterior del lado a→b es (dy, -dx).
                Vector3 outward = new Vector3(b2.y - a2.y, 0f, -(b2.x - a2.x)).normalized;

                AddQuad(
                    submesh,
                    new Vector3(a2.x, baseY, a2.y), new Vector3(b2.x, baseY, b2.y),
                    new Vector3(b2.x, topY, b2.y), new Vector3(a2.x, topY, a2.y),
                    outward,
                    new Vector2(perimeter, baseY) * UvScale,
                    new Vector2(perimeter + length, baseY) * UvScale,
                    new Vector2(perimeter + length, topY) * UvScale,
                    new Vector2(perimeter, topY) * UvScale);

                perimeter += length;
            }
        }

        /// <summary>Añade una tapa horizontal con la forma de la planta.</summary>
        /// <param name="facingUp">Verdadero para una cubierta; falso para un techo visto desde abajo.</param>
        public void AddCap(IReadOnlyList<Vector2> footprint, float y, int submesh, bool facingUp)
        {
            Vector3 normal = facingUp ? Vector3.up : Vector3.down;
            int start = _vertices.Count;
            foreach (Vector2 p in footprint)
            {
                _vertices.Add(new Vector3(p.x, y, p.y));
                _normals.Add(normal);
                _uvs.Add(p * UvScale);
            }

            List<int> triangles = PolygonUtils.Triangulate(footprint);
            for (int i = 0; i < triangles.Count; i += 3)
            {
                AddTriangle(submesh, start + triangles[i], start + triangles[i + 1], start + triangles[i + 2], normal);
            }
        }

        /// <summary>
        /// Añade una losa maciza cuya cara superior sigue una superficie inclinada: paredes desde
        /// <paramref name="baseY"/> hasta la superficie y tapa triangulada con sombreado plano.
        /// </summary>
        /// <param name="footprint">Planta en sentido antihorario.</param>
        /// <param name="topY">Altura de la cara superior en cada punto de la planta.</param>
        public void AddSlab(IReadOnlyList<Vector2> footprint, System.Func<Vector2, float> topY, float baseY, int sideSubmesh, int topSubmesh)
        {
            float perimeter = 0f;
            for (int i = 0; i < footprint.Count; i++)
            {
                Vector2 a = footprint[i];
                Vector2 b = footprint[(i + 1) % footprint.Count];
                float length = Vector2.Distance(a, b);
                Vector3 outward = new Vector3(b.y - a.y, 0f, -(b.x - a.x)).normalized;
                float ta = topY(a);
                float tb = topY(b);
                AddQuad(
                    sideSubmesh,
                    new Vector3(a.x, baseY, a.y), new Vector3(b.x, baseY, b.y),
                    new Vector3(b.x, tb, b.y), new Vector3(a.x, ta, a.y),
                    outward,
                    new Vector2(perimeter, baseY) * UvScale,
                    new Vector2(perimeter + length, baseY) * UvScale,
                    new Vector2(perimeter + length, tb) * UvScale,
                    new Vector2(perimeter, ta) * UvScale);
                perimeter += length;
            }

            List<int> triangles = PolygonUtils.Triangulate(footprint);
            for (int i = 0; i < triangles.Count; i += 3)
            {
                int start = _vertices.Count;
                for (int k = 0; k < 3; k++)
                {
                    Vector2 p = footprint[triangles[i + k]];
                    _vertices.Add(new Vector3(p.x, topY(p), p.y));
                    _uvs.Add(p * UvScale);
                }

                Vector3 normal = Vector3.Cross(_vertices[start + 1] - _vertices[start], _vertices[start + 2] - _vertices[start]).normalized;
                if (normal.y < 0f)
                {
                    normal = -normal;
                }

                for (int k = 0; k < 3; k++)
                {
                    _normals.Add(normal);
                }

                AddTriangle(topSubmesh, start, start + 1, start + 2, normal);
            }
        }

        /// <summary>Añade una caja alineada con los ejes con todas sus caras.</summary>
        public void AddBox(Vector3 center, Vector3 size, int submesh)
        {
            AddOrientedBox(center, size, 0f, submesh);
        }

        /// <summary>Añade la caja alineada con los ejes entre dos esquinas opuestas.</summary>
        public void AddBoxBetween(Vector3 min, Vector3 max, int submesh)
        {
            AddBox((min + max) * 0.5f, max - min, submesh);
        }

        /// <summary>Añade una caja girada <paramref name="yawDegrees"/> alrededor del eje vertical.</summary>
        /// <param name="size">Dimensiones en ejes locales (X ancho, Y alto, Z fondo).</param>
        public void AddOrientedBox(Vector3 center, Vector3 size, float yawDegrees, int submesh)
        {
            Quaternion rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            Vector3 h = size * 0.5f;
            var footprint = new List<Vector2>(4);
            foreach (Vector3 corner in new[]
                     {
                         new Vector3(-h.x, 0f, -h.z), new Vector3(h.x, 0f, -h.z),
                         new Vector3(h.x, 0f, h.z), new Vector3(-h.x, 0f, h.z),
                     })
            {
                Vector3 p = center + rotation * corner;
                footprint.Add(new Vector2(p.x, p.z));
            }

            // El giro puede invertir el sentido del anillo; las paredes necesitan antihorario.
            if (PolygonUtils.SignedArea(footprint) < 0f)
            {
                footprint.Reverse();
            }

            AddWalls(footprint, center.y - h.y, center.y + h.y, submesh);
            AddCap(footprint, center.y + h.y, submesh, facingUp: true);
            AddCap(footprint, center.y - h.y, submesh, facingUp: false);
        }

        /// <summary>Añade una cara plana convexa (triangulada en abanico) con sombreado plano.</summary>
        /// <param name="points">Vértices de la cara, en orden.</param>
        /// <param name="facing">Dirección hacia la que debe mirar la cara.</param>
        /// <param name="uAxis">Dirección de la coordenada U en el plano de la cara.</param>
        public void AddConvexFace(IReadOnlyList<Vector3> points, Vector3 facing, Vector3 uAxis, int submesh)
        {
            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
            if (Vector3.Dot(normal, facing) < 0f)
            {
                normal = -normal;
            }

            Vector3 u = Vector3.ProjectOnPlane(uAxis, normal).normalized;
            Vector3 v = Vector3.Cross(normal, u);
            Vector3 origin = points[0];

            int start = _vertices.Count;
            foreach (Vector3 p in points)
            {
                _vertices.Add(p);
                _normals.Add(normal);
                Vector3 d = p - origin;
                _uvs.Add(new Vector2(Vector3.Dot(d, u) + Vector3.Dot(origin, u), Vector3.Dot(d, v)) * UvScale);
            }

            for (int i = 1; i < points.Count - 1; i++)
            {
                AddTriangle(submesh, start, start + i, start + i + 1, normal);
            }
        }

        /// <summary>
        /// Añade un sólido de revolución alrededor de un eje vertical: cada punto del perfil es
        /// (radio, altura). Cada tramo del perfil tiene sus propios vértices, de modo que las aristas
        /// (bordes de un brocal, molduras) quedan marcadas, y alrededor del eje el sombreado es suave.
        /// </summary>
        /// <param name="profile">Perfil de abajo arriba; radio 0 cierra el sólido en el eje.</param>
        /// <param name="flutes">Número de gallones (0 = superficie lisa).</param>
        /// <param name="fluteDepth">Fracción del radio que se hunde cada gallón entre dos aristas.</param>
        public void AddLathe(Vector3 center, IReadOnlyList<Vector2> profile, int segments, int submesh,
            int flutes = 0, float fluteDepth = 0f)
        {
            Vector3 Point(float angle, Vector2 p)
            {
                float r = p.x;
                if (flutes > 0)
                {
                    r *= 1f - fluteDepth * (1f - Mathf.Abs(Mathf.Cos(angle * flutes * 0.5f)));
                }

                return center + new Vector3(r * Mathf.Cos(angle), p.y, r * Mathf.Sin(angle));
            }

            float step = 2f * Mathf.PI / segments;
            float along = 0f;
            for (int j = 0; j < profile.Count - 1; j++)
            {
                Vector2 p0 = profile[j];
                Vector2 p1 = profile[j + 1];
                float length = Vector2.Distance(p0, p1);
                if (length < 1e-4f)
                {
                    continue;
                }

                int start = _vertices.Count;
                for (int i = 0; i <= segments; i++)
                {
                    float a = i * step;
                    foreach (Vector2 p in new[] { p0, p1 })
                    {
                        Vector3 position = Point(a, p);
                        Vector3 tangent = Point(a + step * 0.25f, p) - Point(a - step * 0.25f, p);
                        if (tangent.sqrMagnitude < 1e-10f)
                        {
                            tangent = new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a));
                        }

                        Vector3 slope = Point(a, p1) - Point(a, p0);
                        Vector3 normal = Vector3.Cross(tangent, slope).normalized;
                        Vector3 radial = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        // En los tramos horizontales (tapas) la normal sale del sentido del perfil, no del radio.
                        if (Vector3.Dot(normal, radial * Mathf.Sign(p1.y - p0.y + 1e-5f) + Vector3.up * Mathf.Sign(p0.x - p1.x)) < 0f)
                        {
                            normal = -normal;
                        }

                        _vertices.Add(position);
                        _normals.Add(normal);
                        float v = p == p0 ? along : along + length;
                        _uvs.Add(new Vector2(a * Mathf.Max(p0.x, p1.x, 0.3f), v) * UvScale);
                    }
                }

                for (int i = 0; i < segments; i++)
                {
                    int a0 = start + i * 2;
                    int b0 = a0 + 2;
                    Vector3 facing = _normals[a0] + _normals[b0 + 1];
                    AddTriangle(submesh, a0, b0, b0 + 1, facing);
                    AddTriangle(submesh, a0, b0 + 1, a0 + 1, facing);
                }

                along += length;
            }
        }

        /// <summary>
        /// Pasa a la submalla <paramref name="target"/> los triángulos de <paramref name="sources"/> con el centro dentro de
        /// alguna de <paramref name="zones"/> que no miran hacia atrás, para cubrirlos con una sola textura (la de una
        /// fachada entera): los que miran hacia <paramref name="facing"/> reciben coordenadas UV de proyección plana sobre
        /// <paramref name="area"/> (de 0 a 1 en X e Y) y los de lado, <paramref name="sideUv"/>, un punto de la textura
        /// con el color liso de la pieza (proyectada, una cara de lado estiraría una sola columna de píxeles).
        /// </summary>
        /// <remarks>
        /// Las zonas y el área se dan en X e Y de la malla. Los triángulos pasados llevan vértices propios, de modo que
        /// los que se quedan conservan sus coordenadas UV.
        /// </remarks>
        public void ProjectFacing(Vector3 facing, Rect area, IReadOnlyList<Rect> zones, Vector2 sideUv, IReadOnlyList<int> sources, int target)
        {
            foreach (int source in sources)
            {
                List<int> triangles = _submeshes[source];
                var kept = new List<int>(triangles.Count);
                for (int t = 0; t < triangles.Count; t += 3)
                {
                    Vector3 a = _vertices[triangles[t]];
                    Vector3 b = _vertices[triangles[t + 1]];
                    Vector3 c = _vertices[triangles[t + 2]];
                    Vector3 normal = Vector3.Cross(b - a, c - a);
                    Vector3 centre = (a + b + c) / 3f;
                    float cosine = normal.sqrMagnitude < 1e-12f ? -1f : Vector3.Dot(normal.normalized, facing);
                    if (cosine < -0.1f || !Contains(zones, centre))
                    {
                        kept.Add(triangles[t]);
                        kept.Add(triangles[t + 1]);
                        kept.Add(triangles[t + 2]);
                        continue;
                    }

                    for (int k = 0; k < 3; k++)
                    {
                        int i = triangles[t + k];
                        Vector3 p = _vertices[i];
                        _vertices.Add(p);
                        _normals.Add(_normals[i]);
                        _uvs.Add(cosine >= 0.5f ? new Vector2((p.x - area.xMin) / area.width, (p.y - area.yMin) / area.height) : sideUv);
                        _submeshes[target].Add(_vertices.Count - 1);
                    }
                }

                _submeshes[source] = kept;
            }
        }

        /// <summary>
        /// Pasa a la submalla <paramref name="target"/> todos los triángulos de <paramref name="sources"/> con la
        /// coordenada UV <paramref name="uv"/>: un punto de color liso de la textura de <paramref name="target"/>.
        /// </summary>
        public void MoveToSubmesh(IReadOnlyList<int> sources, int target, Vector2 uv)
        {
            foreach (int source in sources)
            {
                foreach (int i in _submeshes[source])
                {
                    _vertices.Add(_vertices[i]);
                    _normals.Add(_normals[i]);
                    _uvs.Add(uv);
                    _submeshes[target].Add(_vertices.Count - 1);
                }

                _submeshes[source].Clear();
            }
        }

        private static bool Contains(IReadOnlyList<Rect> zones, Vector3 point)
        {
            foreach (Rect zone in zones)
            {
                if (zone.Contains(new Vector2(point.x, point.y)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Genera la malla conservando solo las submallas con geometría y devuelve sus materiales.</summary>
        /// <remarks>
        /// Cada submalla es una llamada de dibujo; descartar las vacías evita pagar por materiales
        /// que un edificio concreto no usa (toldos, balcones...).
        /// </remarks>
        /// <param name="slots">Material de cada submalla, en el mismo orden que los índices usados.</param>
        /// <param name="materials">Materiales de las submallas conservadas.</param>
        public Mesh BuildCompact(string name, IReadOnlyList<Material> slots, out Material[] materials)
        {
            var used = new List<int>();
            for (int i = 0; i < _submeshes.Count; i++)
            {
                if (_submeshes[i].Count > 0)
                {
                    used.Add(i);
                }
            }

            var compact = new List<List<int>>(used.Count);
            materials = new Material[used.Count];
            for (int i = 0; i < used.Count; i++)
            {
                compact.Add(_submeshes[used[i]]);
                materials[i] = slots[used[i]];
            }

            return CreateMesh(name, compact);
        }

        public Mesh Build(string name)
        {
            return CreateMesh(name, _submeshes);
        }

        private Mesh CreateMesh(string name, List<List<int>> submeshes)
        {
            var mesh = new Mesh
            {
                name = name,
                indexFormat = _vertices.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16,
            };

            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.subMeshCount = submeshes.Count;
            for (int i = 0; i < submeshes.Count; i++)
            {
                mesh.SetTriangles(submeshes[i], i);
            }

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private void AddQuad(int submesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal,
            Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector2 uvD)
        {
            int start = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _vertices.Add(d);
            for (int i = 0; i < 4; i++)
            {
                _normals.Add(normal);
            }

            _uvs.Add(uvA);
            _uvs.Add(uvB);
            _uvs.Add(uvC);
            _uvs.Add(uvD);

            AddTriangle(submesh, start, start + 1, start + 2, normal);
            AddTriangle(submesh, start, start + 2, start + 3, normal);
        }

        /// <summary>Cuadrilátero plano con coordenadas de textura dadas (para degradados que dependen de la altura).</summary>
        public void AddQuadWithUvs(Vector3[] points, Vector2[] uvs, Vector3 facing, int submesh)
        {
            Vector3 normal = Vector3.Cross(points[1] - points[0], points[2] - points[0]).normalized;
            if (Vector3.Dot(normal, facing) < 0f)
            {
                normal = -normal;
            }

            int start = _vertices.Count;
            for (int i = 0; i < 4; i++)
            {
                _vertices.Add(points[i]);
                _normals.Add(normal);
                _uvs.Add(uvs[i]);
            }

            AddTriangle(submesh, start, start + 1, start + 2, normal);
            AddTriangle(submesh, start, start + 2, start + 3, normal);
        }

        /// <summary>
        /// Añade un triángulo orientándolo para que su cara visible apunte hacia <paramref name="facing"/>.
        /// </summary>
        private void AddTriangle(int submesh, int i0, int i1, int i2, Vector3 facing)
        {
            Vector3 n = Vector3.Cross(_vertices[i1] - _vertices[i0], _vertices[i2] - _vertices[i0]);
            List<int> target = _submeshes[submesh];
            target.Add(i0);
            if (Vector3.Dot(n, facing) >= 0f)
            {
                target.Add(i1);
                target.Add(i2);
            }
            else
            {
                target.Add(i2);
                target.Add(i1);
            }
        }
    }
}
