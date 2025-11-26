# Guía de Logs de Debug - Sistema de Corte Progresivo

## 🔍 Cómo usar esta guía

Cuando ejecutes el juego, la consola de Unity mostrará muchos logs. Esta guía te dice exactamente qué logs buscar y qué significan.

---

## 📋 SECUENCIA NORMAL DE LOGS (Si todo funciona bien)

### 1. Al iniciar el juego:

```
[PATTERN START] Pescado Entero iniciado. Tipo: Grid3x3, Prefab asignado: SÍ
[PATTERN START] lime01 iniciado. Tipo: SingleLine, Prefab asignado: SÍ
[PATTERN START] Red_Onion iniciado. Tipo: ConcentricArcs, Prefab asignado: SÍ
```

**✓ Esto significa:** Cada ingrediente tiene el componente IngredientCuttingPattern configurado.

**❌ Si ves:** `Prefab asignado: NO` → Ve al prefab del ingrediente y asigna el CuttingLinePrefab.

---

### 2. Cuando colocas un ingrediente en la tabla:

```
[SNAP PATTERN] ========== GENERANDO PATRÓN DESDE SNAP ZONE ==========
[SNAP PATTERN] Ingrediente que hizo snap: Pescado Entero
[SNAP PATTERN] ✓ Componente IngredientCuttingPattern encontrado!
[SNAP PATTERN]   Tipo de patrón: Grid3x3
[SNAP PATTERN]   Prefab asignado: SÍ
[SNAP PATTERN]   Suscrito a OnPatternCompleted
[SNAP PATTERN]   Llamando a pattern.GeneratePattern()...
```

**✓ Esto significa:** El ingrediente tiene el componente y está a punto de generar líneas.

**❌ Si ves:** `NO TIENE componente IngredientCuttingPattern` → Agrega el componente al prefab.

---

### 3. Durante la generación del patrón:

```
[PATTERN] ========== GENERANDO PATRÓN ==========
[PATTERN] Ingrediente: Pescado Entero
[PATTERN] Tipo de patrón: Grid3x3
[PATTERN] Prefab asignado: CuttingLinePrefab
[PATTERN] Llamando generador específico para Grid3x3...
[PATTERN] → Generando Grid3x3...
```

**✓ Esto significa:** El sistema está llamando al generador correcto.

**❌ Si ves:** `Prefab asignado: NULL - ERROR!` → El prefab no está asignado en el Inspector.

---

### 4. Creación de cada línea individual:

```
[PATTERN CREATE] Creando línea horizontal: Row_1
[PATTERN CREATE]   Start: (-1.52, 1.43, -1.07)
[PATTERN CREATE]   End: (-1.32, 1.43, -1.07)
[PATTERN CREATE]   GameObject instanciado: Row_1, Activo: True
[PATTERN CREATE]   LineRenderer encontrado en prefab
[PATTERN CREATE]   LineRenderer configurado: 2 posiciones
[PATTERN CREATE]   UseWorldSpace: False
[PATTERN CREATE]   Material asignado: LineMaterial
[PATTERN CREATE]   Collider agregado: Trigger=True, Size=(0.20, 0.01, 0.01)
[PATTERN CREATE]   CuttingLine component encontrado y configurado
[PATTERN CREATE]   ✓ Línea agregada a la lista. Total líneas ahora: 1
```

**Este log se repite 6 veces para el pescado (3 filas + 3 columnas).**

**✓ Esto significa:** Las líneas se están creando correctamente.

**❌ Problemas comunes:**
- `Prefab no tiene LineRenderer` → El prefab no tiene el componente LineRenderer
- `Sin material! La línea puede no ser visible` → Asigna el LineMaterial
- `Prefab no tiene componente CuttingLine!` → El prefab necesita el script CuttingLine.cs

---

### 5. Después de crear todas las líneas:

```
[PATTERN] Total de líneas creadas: 6
[PATTERN] Configurando callbacks para 6 líneas...
[PATTERN]   - Callback configurado para: Row_1
[PATTERN]   - Callback configurado para: Row_2
[PATTERN]   - Callback configurado para: Row_3
[PATTERN]   - Callback configurado para: Col_1
[PATTERN]   - Callback configurado para: Col_2
[PATTERN]   - Callback configurado para: Col_3
[PATTERN] Activando primera línea: Row_1
```

**✓ Esto significa:** Todas las líneas fueron creadas y la primera está activándose.

**❌ Si ves:** `Total de líneas creadas: 0` → El generador no está funcionando.

---

### 6. Cuando se activa la primera línea:

```
[CUTTING LINE AWAKE] Row_1 - Awake llamado
[CUTTING LINE] LineRenderer encontrado en Row_1
[CUTTING LINE] LineRenderer configurado: Width=0.004
[CUTTING LINE START] Row_1 - Start llamado, desactivando línea inicialmente
[CUTTING LINE SET ACTIVE] Row_1 - SetActive(True) llamado
[CUTTING LINE SET ACTIVE]   LineRenderer.enabled = True
[CUTTING LINE SET ACTIVE]   LineRenderer tiene 2 posiciones
[CUTTING LINE SET ACTIVE]   Posición 0: (0.10, 0.00, 0.10)
[CUTTING LINE SET ACTIVE]   Posición 1: (0.30, 0.00, 0.10)
[CUTTING LINE SET ACTIVE]   ✓ Línea activada y visible: Row_1
```

**✓ Esto significa:** La línea está activa y debería ser VISIBLE en el juego.

**❌ Si ves:** `LineRenderer es NULL!` → Algo salió mal en la creación.

---

### 7. Final de la generación:

```
[PATTERN] ========== PATRÓN GENERADO EXITOSAMENTE ==========
[PATTERN] Total líneas: 6, Primera línea activa: Row_1
[SNAP PATTERN] ✓ Patrón de corte generado para Pescado Entero: Grid3x3
[SNAP PATTERN] ========== FIN GENERACIÓN PATRÓN ==========
```

**✓ Esto significa:** TODO FUNCIONÓ. La línea debería estar visible en el juego.

---

## 🚨 ERRORES COMUNES Y SOLUCIONES

### Error 1: "NO TIENE cuttingLinePrefab asignado"
```
[PATTERN ERROR] Pescado Entero NO TIENE cuttingLinePrefab asignado!
```
**Solución:**
1. Selecciona el prefab del ingrediente (Pescado Entero, lime01, etc.)
2. En el componente IngredientCuttingPattern
3. Arrastra el CuttingLinePrefab al campo "Cutting Line Prefab"

---

### Error 2: "NO TIENE componente IngredientCuttingPattern"
```
[SNAP PATTERN ERROR] ¡Pescado Entero NO TIENE componente IngredientCuttingPattern!
```
**Solución:**
1. Selecciona el prefab del ingrediente
2. Add Component → IngredientCuttingPattern
3. Configura el Pattern Type (Grid3x3, SingleLine, etc.)

---

### Error 3: "Total de líneas creadas: 0"
```
[PATTERN] Total de líneas creadas: 0
[PATTERN ERROR] ¡No se crearon líneas!
```
**Posibles causas:**
1. El prefab CuttingLinePrefab no está asignado
2. El método generador (GenerateGrid3x3, etc.) tiene un error
3. Los bounds del ingrediente son muy pequeños

**Solución:**
- Revisa los logs anteriores para ver si hay errores en [PATTERN CREATE]
- Asegúrate que el ingrediente tiene un MeshRenderer o Collider

---

### Error 4: La línea se crea pero no es visible

Si ves todos los logs de éxito pero no ves la línea en el juego:

**Causas posibles:**
1. **Sin material:**
   ```
   [PATTERN CREATE]   ¡Sin material! La línea puede no ser visible.
   ```
   Solución: Crea y asigna el LineMaterial

2. **Línea muy delgada:**
   - Aumenta `Base Width` en CuttingLine.cs de 0.004 a 0.01

3. **Línea dentro del ingrediente:**
   - Aumenta `Line Height Offset` de 0.002 a 0.01

4. **UseWorldSpace = True:**
   - Debe ser False. Revisa el log:
   ```
   [PATTERN CREATE]   UseWorldSpace: False
   ```

5. **Posiciones en (0,0,0):**
   - Revisa las posiciones en los logs:
   ```
   [CUTTING LINE SET ACTIVE]   Posición 0: (0.00, 0.00, 0.00)
   [CUTTING LINE SET ACTIVE]   Posición 1: (0.00, 0.00, 0.00)
   ```
   Si ambas son (0,0,0), el ingrediente no tiene bounds correctos.

---

## 🔧 CHECKLIST DE VERIFICACIÓN

Usa esta lista para verificar tu configuración:

### ✅ En cada Ingrediente (Prefab):
- [ ] Tiene componente `IngredientCuttingPattern`
- [ ] `Pattern Type` configurado (Grid3x3, SingleLine, etc.)
- [ ] `Cutting Line Prefab` asignado
- [ ] `Line Material` asignado
- [ ] `Line Height Offset` = 0.002 (o más si no ves las líneas)

### ✅ En CuttingLinePrefab:
- [ ] Tiene componente `LineRenderer`
- [ ] Tiene componente `CuttingLine`
- [ ] LineRenderer Width = 0.004
- [ ] LineRenderer Use World Space = **FALSE**

### ✅ Material LineMaterial:
- [ ] Shader = Sprites/Default o URP/Unlit
- [ ] Emission activado
- [ ] Color naranja brillante

---

## 📊 LOGS PARA CADA INGREDIENTE

### Pescado (Grid3x3):
- Debería crear 6 líneas: Row_1, Row_2, Row_3, Col_1, Col_2, Col_3
- Solo Row_1 debe estar activa inicialmente

### Limón (SingleLine):
- Debería crear 1 línea: CenterLine
- Debe estar activa inmediatamente

### Cebolla (ConcentricArcs):
- Debería crear 4 líneas: Arc_1, Arc_2, Arc_3, Arc_4
- Solo Arc_1 debe estar activa inicialmente

### Ají (MultiTouch):
- Debería crear 8 líneas: Touch_1 hasta Touch_8
- Solo Touch_1 debe estar activa inicialmente

---

## 🎯 CÓMO DEBUGGEAR PASO A PASO

1. **Ejecuta el juego**
2. **Busca en la consola:** `[PATTERN START]`
   - Si no aparece → El ingrediente no tiene el script
3. **Coloca el ingrediente en la tabla**
4. **Busca:** `[SNAP PATTERN] ========== GENERANDO`
   - Si no aparece → El snap no está funcionando
5. **Busca:** `[PATTERN] ========== GENERANDO PATRÓN`
   - Si no aparece → El componente no está configurado
6. **Busca:** `[PATTERN CREATE]` (debe aparecer varias veces)
   - Si no aparece → El generador no está funcionando
7. **Busca:** `[CUTTING LINE SET ACTIVE] ... ✓ Línea activada`
   - Si aparece → La línea DEBERÍA ser visible
8. **Si la línea no es visible pero los logs son correctos:**
   - Revisa el material
   - Aumenta el width
   - Aumenta el height offset
   - Verifica las posiciones en los logs

---

## 💡 TIPS

- **Filtra los logs:** En la consola de Unity, escribe `[PATTERN]` en la barra de búsqueda
- **Modo Scene View:** Puedes ver las líneas en el Scene View incluso en Play mode
- **Gizmos:** Asegúrate que los Gizmos estén activados en el Game View
- **Layer:** Las líneas deberían estar en el layer "CuttingLines"

---

¡Con estos logs podrás identificar exactamente dónde está el problema!
