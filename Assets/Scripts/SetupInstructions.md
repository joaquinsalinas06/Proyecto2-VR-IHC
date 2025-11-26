# Instrucciones de Configuración del Sistema de Corte Progresivo

## 1. Crear Prefab de Línea de Corte

### Paso 1: Crear GameObject vacío
1. En Unity, clic derecho en Hierarchy → Create Empty
2. Nombre: `CuttingLinePrefab`

### Paso 2: Agregar componentes
1. Selecciona `CuttingLinePrefab`
2. Add Component → Line Renderer
   - Width: 0.004
   - Material: Crear nuevo material (ver sección de materiales)
   - Positions: 2
   - Use World Space: **DESACTIVADO** (false)

3. Add Component → `CuttingLine` (el script que creamos)
   - Required Cut Time: 1.5
   - Completion Threshold: 0.8
   - Min Cut Velocity: 0.3
   - Knife Tag: "Knife"

4. Add Component → Audio Source
   - Spatial Blend: 1.0 (3D)
   - Loop: true
   - Volume: 0.3

### Paso 3: Guardar como Prefab
1. Arrastra `CuttingLinePrefab` a la carpeta Assets/Prefabs
2. Elimina el GameObject de la escena (ya está guardado como prefab)

---

## 2. Crear Material para Líneas

### Material: LineMaterial
1. Clic derecho en Assets → Create → Material
2. Nombre: `LineMaterial`
3. Configuración:
   - Shader: Sprites/Default (o Universal Render Pipeline/Unlit)
   - Rendering Mode: Transparent
   - Color: Naranja brillante (1, 0.3, 0.1, 1)
   - Emission: **ACTIVADO**
   - Emission Color: Naranja (1, 0.3, 0.1) con intensidad 0.5

---

## 3. Configurar Ingredientes

Para cada ingrediente (Pescado, Limón, Cebolla, Ají):

### Paso 1: Agregar componente IngredientCuttingPattern
1. Selecciona el prefab del ingrediente
2. Add Component → `IngredientCuttingPattern`

### Paso 2: Configurar según tipo

**Pescado Entero:**
- Pattern Type: Grid3x3
- Cutting Line Prefab: Arrastra `CuttingLinePrefab`
- Line Material: Arrastra `LineMaterial`
- Line Height Offset: 0.002
- Grid Spacing: 0.1

**Limón (lime01):**
- Pattern Type: SingleLine
- Cutting Line Prefab: Arrastra `CuttingLinePrefab`
- Line Material: Arrastra `LineMaterial`
- Line Height Offset: 0.002

**Cebolla:**
- Pattern Type: ConcentricArcs
- Cutting Line Prefab: Arrastra `CuttingLinePrefab`
- Line Material: Arrastra `LineMaterial`
- Line Height Offset: 0.002
- Arc Count: 4
- Arc Base Radius: 0.03

**Ají:**
- Pattern Type: MultiTouch
- Cutting Line Prefab: Arrastra `CuttingLinePrefab`
- Line Material: Arrastra `LineMaterial`
- Line Height Offset: 0.002
- Touch Point Count: 8
- Touch Circle Radius: 0.01

---

## 4. Configurar el Cuchillo (couteau)

1. Selecciona el GameObject del cuchillo
2. En el componente `KnifeController`:
   - Raycast Max Distance: 0.5
   - Cutting Line Layer: Configurar después de crear el layer

### Crear Layer para Líneas de Corte
1. Edit → Project Settings → Tags and Layers
2. En "Layers", encuentra un slot vacío (ej: Layer 8)
3. Nombre: `CuttingLines`
4. Vuelve al cuchillo, en `Cutting Line Layer` selecciona "CuttingLines"

### Configurar Tag del Cuchillo
1. Si el cuchillo no tiene tag "Knife":
   - Selecciona el cuchillo
   - En Inspector, Tag → Add Tag
   - Crear nuevo tag: `Knife`
   - Asignar el tag al cuchillo

---

## 5. Configurar CuttingBoardSnapZone

No requiere cambios adicionales - el script ya fue modificado para detectar automáticamente los patrones.

---

## 6. Testing

### Prueba con el Limón (más simple):
1. Play
2. Coloca el limón en la tabla de cortar
3. Debe aparecer UNA línea naranja vertical por el centro
4. Pasa el cuchillo sobre la línea varias veces
5. La línea debe cambiar de color: Naranja → Amarillo → Verde
6. Al completarse (80%+), debe sonar un "ding" y aparecer las 2 mitades

### Prueba con el Pescado (más complejo):
1. Coloca el pescado en la tabla
2. Debe aparecer SOLO la primera fila horizontal (naranja)
3. Córtala (pasa el cuchillo sobre ella varias veces)
4. Al completarse, debe aparecer la segunda fila
5. Repite hasta completar las 3 filas
6. Luego aparecen las 3 columnas verticales (una por una)
7. Al final, el pescado se divide en cubos

---

## 7. Ajustes Opcionales

### Si las líneas son muy delgadas:
- En `CuttingLine.cs`, cambiar `baseWidth` a 0.006 o 0.008

### Si el corte es muy lento:
- En `IngredientCuttingPattern` de cada ingrediente, reducir `Required Cut Time` a 1.0

### Si el corte es muy estricto:
- En `CuttingLine.cs`, cambiar `Completion Threshold` de 0.8 a 0.7

### Si el raycast no detecta las líneas:
- Asegurarse que el layer "CuttingLines" está configurado
- En el Inspector del `CuttingLinePrefab`, verificar que el Layer sea "CuttingLines"
- En el cuchillo, verificar que `Cutting Line Layer` incluye "CuttingLines"

---

## 8. Troubleshooting

**Problema: Las líneas no aparecen**
- Verifica que los ingredientes tienen el componente `IngredientCuttingPattern`
- Verifica que `Cutting Line Prefab` está asignado
- Revisa la consola para mensajes de error

**Problema: El cuchillo no detecta las líneas**
- Verifica que el Layer "CuttingLines" existe
- Verifica que el cuchillo tiene `Cutting Line Layer` configurado
- Asegúrate que el cuchillo tiene tag "Knife"

**Problema: Las líneas no cambian de color**
- Verifica que el cuchillo se está moviendo con suficiente velocidad (>0.3 m/s)
- Revisa los logs: debe decir `[CUT LINE] progreso: X.XX`

**Problema: Las líneas están dentro del ingrediente (no visibles)**
- Aumenta `Line Height Offset` a 0.005 o 0.01

**Problema: Las líneas están muy arriba**
- Reduce `Line Height Offset` a 0.001

---

## 9. Flujo Completo del Sistema

1. Usuario coloca ingrediente en tabla → SnapIngredient()
2. Se genera el patrón de líneas → GenerateCuttingPattern()
3. Aparece solo la primera línea (activa), las demás invisibles
4. Usuario mueve cuchillo sobre la línea
5. KnifeController raycast detecta la línea → RegisterCut()
6. Progreso aumenta, color cambia gradualmente
7. Al llegar a 80%+ → OnLineCompleted()
8. Siguiente línea se activa (si hay más)
9. Al completar todas las líneas → OnPatternCompleted()
10. Se ejecuta el corte final → OnIngredientCut()
11. Ingrediente se destruye, aparecen los pedazos

---

## 10. Colores Recomendados

- **Empty Color** (Naranja rojizo): RGB(255, 77, 26) o (1, 0.3, 0.1)
- **Filling Color** (Amarillo): RGB(255, 230, 51) o (1, 0.9, 0.2)
- **Complete Color** (Verde brillante): RGB(51, 255, 77) o (0.2, 1, 0.3)

---

Sigue estos pasos en orden y el sistema debería funcionar correctamente. ¡Buena suerte!
