using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Mur invisible "propre" qui suit une ligne de points : les ENFANTS de cet objet, dans l'ordre
// de la Hierarchy.
// - La ligne est lissée entre les points (courbe), elle suit les formes organiques de l'île.
// - Le mur est continu et a une épaisseur : pas de trou aux angles, pas d'arête où le joueur accroche.
// - Le bas du mur est enfoncé dans le sol et le haut suit le relief.
// - Visible seulement dans la Scene view (traits verts). En jeu, il n'y a que le collider.
//
// Utilisation : crée un objet vide, ajoute ce composant, puis clic droit sur le composant
// (ou les 3 points) > "Créer 8 points en cercle" ou "Ajouter un point". Déplace les points
// dans la Scene view. Mets l'objet sur un calque dédié (voir la Layer Collision Matrix).
[DisallowMultipleComponent]
public class InvisibleWall : MonoBehaviour
{
    [Tooltip("Décoche pour n'avoir qu'un contour sans mur (ex. zone de jeu utilisée par BallRespawn)")]
    public bool creerCollider = true;

    [Header("Forme")]
    [Tooltip("Relier le dernier point au premier (contour fermé, ex. tour de l'île)")]
    public bool ferme = true;

    [Tooltip("Lisser la ligne entre les points (courbe)")]
    public bool lisser = true;

    [Tooltip("Nombre de segments entre deux points quand la ligne est lissée")]
    [Range(1, 12)] public int subdivisions = 4;

    [Header("Dimensions (m)")]
    public float hauteur = 3f;
    [Tooltip("Partie enfoncée sous le sol (évite de passer dessous)")]
    public float profondeur = 1f;
    public float epaisseur = 0.5f;

    [Header("Sol")]
    [Tooltip("Le mur suit le relief : on cherche le sol sous chaque point")]
    public bool suivreLeSol = true;
    [Tooltip("Calques considérés comme le sol (le terrain). Ne mets pas le calque des murs ici.")]
    public LayerMask coucheSol = 1; // Default
    public float hauteurRecherche = 100f;

    [Header("Debug")]
    [Tooltip("Matériau optionnel pour voir le mur en jeu (laisser vide = invisible)")]
    public Material materiauDebug;

    private Mesh mesh;

    private void Awake()
    {
        if (creerCollider)
            Construire();
    }

    // Ligne lissée en coordonnées monde (utilisée par BallRespawn pour savoir si la balle est dans la zone)
    public List<Vector3> ContourMonde()
    {
        return Echantillonner();
    }

    // ---------- Construction du collider ----------

    [ContextMenu("Reconstruire le mur (en jeu)")]
    public void Construire()
    {
        var echantillons = Echantillonner();
        int n = echantillons.Count;
        if (n < 2)
            return;

        var bases = new float[n];
        for (int i = 0; i < n; i++)
            bases[i] = HauteurSol(echantillons[i]);

        var vertices = new List<Vector3>(n * 4 + 8);
        var triangles = new List<int>(n * 24);

        // 4 sommets par échantillon : extérieur bas/haut, intérieur bas/haut
        var ob = new int[n]; var ot = new int[n]; var ib = new int[n]; var it = new int[n];
        var normales = new Vector3[n];
        for (int i = 0; i < n; i++)
        {
            Vector3 normale = NormaleHorizontale(echantillons, i);
            normales[i] = normale;
            Vector3 p = echantillons[i];
            Vector3 dehors = normale * (epaisseur * 0.5f);

            Vector3 bas = new Vector3(p.x, bases[i] - profondeur, p.z);
            Vector3 haut = new Vector3(p.x, bases[i] + hauteur, p.z);

            ob[i] = Ajouter(vertices, bas + dehors);
            ot[i] = Ajouter(vertices, haut + dehors);
            ib[i] = Ajouter(vertices, bas - dehors);
            it[i] = Ajouter(vertices, haut - dehors);
        }

        int nbSegments = ferme ? n : n - 1;
        for (int s = 0; s < nbSegments; s++)
        {
            int a = s;
            int b = (s + 1) % n;
            Vector3 normaleSegment = (normales[a] + normales[b]).normalized;

            AjouterQuad(vertices, triangles, ob[a], ot[a], ot[b], ob[b], normaleSegment);   // face extérieure
            AjouterQuad(vertices, triangles, ib[a], it[a], it[b], ib[b], -normaleSegment);  // face intérieure
            AjouterQuad(vertices, triangles, ot[a], it[a], it[b], ot[b], Vector3.up);       // dessus
            AjouterQuad(vertices, triangles, ob[a], ib[a], ib[b], ob[b], Vector3.down);     // dessous
        }

        if (!ferme)
        {
            // Bouchons aux deux bouts
            Vector3 debut = (echantillons[0] - echantillons[1]); debut.y = 0f;
            Vector3 fin = (echantillons[n - 1] - echantillons[n - 2]); fin.y = 0f;
            AjouterQuad(vertices, triangles, ob[0], ot[0], it[0], ib[0], debut.normalized);
            AjouterQuad(vertices, triangles, ob[n - 1], ot[n - 1], it[n - 1], ib[n - 1], fin.normalized);
        }

        // Passage en coordonnées locales de cet objet
        for (int i = 0; i < vertices.Count; i++)
            vertices[i] = transform.InverseTransformPoint(vertices[i]);

        if (mesh == null)
        {
            mesh = new Mesh { name = name + "_Mur" };
            mesh.hideFlags = HideFlags.DontSave;
        }
        mesh.Clear();
        if (vertices.Count > 65000)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var col = GetComponent<MeshCollider>();
        if (col == null)
            col = gameObject.AddComponent<MeshCollider>();
        col.convex = false;
        col.sharedMesh = null;
        col.sharedMesh = mesh;

        if (materiauDebug != null)
        {
            var mf = GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = GetComponent<MeshRenderer>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = materiauDebug;
        }
    }

    private static int Ajouter(List<Vector3> vertices, Vector3 v)
    {
        vertices.Add(v);
        return vertices.Count - 1;
    }

    // Ajoute un quad en vérifiant que sa face regarde bien vers "orientation"
    private static void AjouterQuad(List<Vector3> v, List<int> t, int a, int b, int c, int d, Vector3 orientation)
    {
        Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (Vector3.Dot(n, orientation) < 0f)
        {
            int tmp = b; b = d; d = tmp;
        }
        t.Add(a); t.Add(b); t.Add(c);
        t.Add(a); t.Add(c); t.Add(d);
    }

    // ---------- Géométrie de la ligne ----------

    private List<Vector3> PointsControle()
    {
        var points = new List<Vector3>(transform.childCount);
        for (int i = 0; i < transform.childCount; i++)
            points.Add(transform.GetChild(i).position);
        return points;
    }

    private List<Vector3> Echantillonner()
    {
        var p = PointsControle();
        int n = p.Count;
        var resultat = new List<Vector3>();
        if (n < 2)
            return p;

        if (!lisser || n < 3)
        {
            resultat.AddRange(p);
            return resultat;
        }

        int nbSegments = ferme ? n : n - 1;
        for (int i = 0; i < nbSegments; i++)
        {
            Vector3 p0 = Point(p, i - 1);
            Vector3 p1 = Point(p, i);
            Vector3 p2 = Point(p, i + 1);
            Vector3 p3 = Point(p, i + 2);
            for (int s = 0; s < subdivisions; s++)
                resultat.Add(CatmullRom(p0, p1, p2, p3, s / (float)subdivisions));
        }
        if (!ferme)
            resultat.Add(p[n - 1]);

        return resultat;
    }

    private Vector3 Point(List<Vector3> p, int i)
    {
        int n = p.Count;
        if (ferme)
            return p[((i % n) + n) % n];
        return p[Mathf.Clamp(i, 0, n - 1)];
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t, t3 = t2 * t;
        return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    private Vector3 NormaleHorizontale(List<Vector3> e, int i)
    {
        int n = e.Count;
        Vector3 avant, apres;
        if (ferme)
        {
            avant = e[(i - 1 + n) % n];
            apres = e[(i + 1) % n];
        }
        else
        {
            avant = e[Mathf.Max(i - 1, 0)];
            apres = e[Mathf.Min(i + 1, n - 1)];
        }
        Vector3 tangente = apres - avant;
        tangente.y = 0f;
        if (tangente.sqrMagnitude < 0.000001f)
            tangente = Vector3.forward;
        return Vector3.Cross(Vector3.up, tangente.normalized);
    }

    private float HauteurSol(Vector3 p)
    {
        if (suivreLeSol)
        {
            Vector3 depart = new Vector3(p.x, p.y + hauteurRecherche, p.z);
            if (Physics.Raycast(depart, Vector3.down, out RaycastHit hit, hauteurRecherche * 2f, coucheSol,
                                QueryTriggerInteraction.Ignore))
                return hit.point.y;
        }
        return p.y;
    }

    // ---------- Aperçu dans la Scene view ----------

    private void OnDrawGizmos()
    {
        // Vert = mur, cyan = simple contour de zone (sans collider)
        DessinerApercu(creerCollider ? new Color(0.2f, 1f, 0.3f, 0.8f) : new Color(0.2f, 0.9f, 1f, 0.8f));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        for (int i = 0; i < transform.childCount; i++)
            Gizmos.DrawSphere(transform.GetChild(i).position, 0.3f);
    }

    private void DessinerApercu(Color couleur)
    {
        var e = Echantillonner();
        int n = e.Count;
        if (n < 2)
            return;

        Gizmos.color = couleur;
        int nbSegments = ferme ? n : n - 1;
        for (int s = 0; s < nbSegments; s++)
        {
            Vector3 a = e[s], b = e[(s + 1) % n];
            float ya = HauteurSol(a), yb = HauteurSol(b);
            Vector3 aBas = new Vector3(a.x, ya, a.z), bBas = new Vector3(b.x, yb, b.z);
            Vector3 aHaut = aBas + Vector3.up * hauteur, bHaut = bBas + Vector3.up * hauteur;
            Gizmos.DrawLine(aBas, bBas);
            Gizmos.DrawLine(aHaut, bHaut);
            Gizmos.DrawLine(aBas, aHaut);
        }
    }

#if UNITY_EDITOR
    // ---------- Outils dans l'éditeur (clic droit sur le composant) ----------

    [ContextMenu("Ajouter un point")]
    private void AjouterPoint()
    {
        Vector3 position = transform.position;
        int n = transform.childCount;
        if (n >= 2)
        {
            Vector3 dernier = transform.GetChild(n - 1).position;
            Vector3 avantDernier = transform.GetChild(n - 2).position;
            position = dernier + (dernier - avantDernier).normalized * 3f;
        }
        else if (n == 1)
        {
            position = transform.GetChild(0).position + Vector3.forward * 3f;
        }

        var point = new GameObject("Point " + n);
        Undo.RegisterCreatedObjectUndo(point, "Ajouter un point");
        point.transform.SetParent(transform, false);
        point.transform.position = position;
        Selection.activeGameObject = point;
    }

    [ContextMenu("Créer 8 points en cercle")]
    private void CreerCercle()
    {
        const int nb = 8;
        const float rayon = 20f;
        for (int i = 0; i < nb; i++)
        {
            float angle = i * Mathf.PI * 2f / nb;
            var point = new GameObject("Point " + transform.childCount);
            Undo.RegisterCreatedObjectUndo(point, "Créer des points");
            point.transform.SetParent(transform, false);
            point.transform.position = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * rayon;
        }
    }

    [ContextMenu("Poser les points sur le sol")]
    private void PoserSurLeSol()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform t = transform.GetChild(i);
            Vector3 p = t.position;
            Vector3 depart = new Vector3(p.x, p.y + hauteurRecherche, p.z);
            if (Physics.Raycast(depart, Vector3.down, out RaycastHit hit, hauteurRecherche * 2f, coucheSol,
                                QueryTriggerInteraction.Ignore))
            {
                Undo.RecordObject(t, "Poser sur le sol");
                t.position = hit.point;
            }
        }
    }
#endif
}
