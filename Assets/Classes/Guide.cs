using System.Collections.Generic;
[System.Serializable]
public partial class Guide
{
    public string titulo;

    public string explicacion;

    public List<Paso> pasos;
    public List<GuideMaterial> materiales;
    public int tiempo_insumido;
    public float costo;
}