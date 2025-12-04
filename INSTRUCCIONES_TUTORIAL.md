# 📚 Sistema de Tutorial e Instrucciones - Guía de Configuración

Este documento explica cómo configurar el sistema de tutoriales e instrucciones para tu experiencia VR de cocina peruana.

## 🎯 Componentes del Sistema

### 1. **TutorialManager** - Panel de Bienvenida Inicial
Panel que se muestra al inicio del juego para explicar los controles básicos.

### 2. **InGameInstructionPanel** - Instrucciones Durante el Juego
Panel que muestra qué ingrediente colocar y cómo cortarlo según el paso actual.

---

## 🚀 Configuración Paso a Paso

### PARTE 1: Panel de Bienvenida (TutorialManager)

#### Paso 1: Crear el Canvas de Tutorial

1. En la escena donde quieres el tutorial (ej: `paso1.unity` o `LomoSaltadoScene.unity`):
   - Click derecho en Hierarchy → UI → Canvas
   - Renombra el Canvas a `TutorialCanvas`

2. Configurar el Canvas para VR:
   - Selecciona `TutorialCanvas`
   - En Inspector → Canvas:
     - **Render Mode**: `World Space`
     - **Event Camera**: Arrastra tu cámara VR (ej: `CenterEyeAnchor`)
   - En Rect Transform:
     - **Pos X, Y, Z**: `(0, 1.5, 2)` (frente al jugador, a altura de ojos)
     - **Rotation**: `(0, 0, 0)`
     - **Scale**: `(0.001, 0.001, 0.001)` (para que sea del tamaño adecuado en VR)
     - **Width**: `1920`
     - **Height**: `1080`

#### Paso 2: Crear la Estructura de UI

Dentro de `TutorialCanvas`, crea esta jerarquía:

```
TutorialCanvas
├── Panel_Background (Image - fondo oscuro semi-transparente)
│   ├── Title_Text (TextMeshPro - Título del tutorial)
│   ├── Description_Text (TextMeshPro - Descripción/instrucciones)
│   ├── Illustration_Image (Image - Imagen de ejemplo/icono)
│   ├── PageCounter_Text (TextMeshPro - "Página 1/3")
│   ├── Button_Previous (Button con TextMeshPro - "Anterior")
│   └── Button_Next (Button con TextMeshPro - "Siguiente")
```

**Configuración visual recomendada:**
- **Panel_Background**:
  - Color: Negro con Alpha 0.8
  - Anchors: Stretch (para que ocupe todo el canvas)
  - Offset Left/Right/Top/Bottom: 100 (margen)

- **Title_Text**:
  - Font Size: 72
  - Color: Blanco
  - Alignment: Center
  - Position: Top del panel

- **Description_Text**:
  - Font Size: 48
  - Color: Blanco
  - Alignment: Center/Middle
  - Position: Centro del panel
  - **¡IMPORTANTE!**: Marca "Enable Word Wrapping"

- **PageCounter_Text**:
  - Font Size: 36
  - Position: Bottom Right

- **Botones**:
  - Font Size: 42
  - Previous: Bottom Left
  - Next: Bottom Right

#### Paso 3: Agregar el Script TutorialManager

1. Selecciona un GameObject vacío (puede ser el `TutorialCanvas` mismo)
   - Add Component → `TutorialManager`

2. En el Inspector, configura:

**UI References:**
- Tutorial Canvas: Arrastra `TutorialCanvas`
- Title Text: Arrastra `Title_Text`
- Description Text: Arrastra `Description_Text`
- Illustration Image: Arrastra `Illustration_Image`
- Next Button: Arrastra `Button_Next`
- Previous Button: Arrastra `Button_Previous`
- Page Counter Text: Arrastra `PageCounter_Text`

**Tutorial Pages:**
- Haz click en el **"+"** para agregar páginas

**Ejemplo de 3 páginas:**

**Página 0:**
```
Title: "¡Bienvenido a la Experiencia VR de Cocina Peruana!"
Description:
"En esta experiencia aprenderás a preparar platos típicos peruanos.

IMPORTANTE: Esta experiencia usa HAND TRACKING (seguimiento de manos).

🎮 Los CONTROLADORES solo sirven para MOVERTE en el espacio.
🖐️ Usa tus MANOS REALES para AGARRAR herramientas e ingredientes."

Illustration: (Imagen de controladores Meta Quest - opcional)
```

**Página 1:**
```
Title: "Cómo Moverte"
Description:
"🎮 CONTROLADORES:
- Joystick Izquierdo: Girar cámara
- Joystick Derecho: Teletransporte
- Botones A/B/X/Y: Navegar menús

NO intentes agarrar objetos con los controladores."

Illustration: (Imagen de controles Meta Quest)
```

**Página 2:**
```
Title: "Cómo Interactuar con las Manos"
Description:
"🖐️ MANOS:
- Pellizcar (juntar índice y pulgar): Agarrar objetos
- Soltar pellizco: Soltar objeto
- Agarrar herramientas y colócalas donde se indica

¡Ahora sí, empecemos a cocinar!"

Illustration: (Imagen de gesto de pellizco)
```

**Navigation:**
- Next Page Button: `Button.One` (Botón A en Meta Quest)
- Previous Page Button: `Button.Two` (Botón B en Meta Quest)
- Navigation Controller: `RTouch` (Controlador derecho)

**Auto-Hide:**
- Auto Hide On Complete: ✓ (marcado)
- Hide Delay: `2` segundos

---

### PARTE 2: Instrucciones Durante el Juego (InGameInstructionPanel)

#### Paso 1: Crear el Canvas de Instrucciones

1. En la misma escena:
   - Click derecho en Hierarchy → UI → Canvas
   - Renombra a `InstructionCanvas`

2. Configurar el Canvas:
   - **Render Mode**: `World Space`
   - **Event Camera**: Tu cámara VR
   - Posición: `(0, 2, 1.5)` (arriba del jugador, visible)
   - Rotation: `(30, 0, 0)` (inclinado hacia abajo)
   - Scale: `(0.001, 0.001, 0.001)`

#### Paso 2: Crear la UI del Panel de Instrucciones

```
InstructionCanvas
├── Panel_Instruction (Image - fondo semi-transparente)
│   ├── Title_Text (TextMeshPro - "Paso 1: Cortar Pescado")
│   ├── Description_Text (TextMeshPro - "Coloca el pescado en la tabla")
│   ├── IngredientIcon_Image (Image - Icono del ingrediente)
│   ├── CutTypeIcon_Image (Image - Icono del tipo de corte)
│   ├── ProgressSlider (Slider - Barra de progreso)
│   └── CompletionMessage (GameObject con TextMeshPro - "¡Paso Completado!")
```

**Configuración visual:**
- **Panel_Instruction**:
  - Color: Azul oscuro con Alpha 0.7
  - Width: 1200, Height: 400

- **Title_Text**:
  - Font Size: 64, Color: Amarillo
  - Top del panel

- **Description_Text**:
  - Font Size: 52, Color: Blanco
  - Centro del panel

- **ProgressSlider**:
  - Bottom del panel
  - Min: 0, Max: 1
  - Fill Color: Verde

- **CompletionMessage**:
  - Desactivado al inicio
  - Font Size: 80, Color: Verde brillante
  - Text: "✅ ¡Paso Completado!"

#### Paso 3: Agregar el Script InGameInstructionPanel

1. Selecciona un GameObject vacío (puede ser `InstructionCanvas`)
   - Add Component → `InGameInstructionPanel`

2. Configurar el script:

**Referencias:**
- Cutting Board: Arrastra el GameObject de tu tabla de cortar (tiene `CuttingBoardSnapZone`)
- Instruction Panel: Arrastra `Panel_Instruction`
- Instruction Title Text: Arrastra `Title_Text`
- Instruction Description Text: Arrastra `Description_Text`
- Ingredient Icon Image: Arrastra `IngredientIcon_Image`
- Cut Type Icon Image: Arrastra `CutTypeIcon_Image`
- Panel Background Image: Arrastra `Panel_Instruction` (el Image component)
- Progress Slider: Arrastra `ProgressSlider`
- Completion Message: Arrastra `CompletionMessage`

**Step Instructions:**

Debes crear UNA instrucción por cada paso que tengas en tu `CuttingBoardSnapZone`.

**Ejemplo para Ceviche (3 pasos):**

**Step 0 - Pescado:**
```
Step Name: "Cortar Pescado"
Place Instruction Text: "🐟 Coloca el PESCADO ENTERO en la tabla de cortar"
Cut Instruction Text: "🔪 Corta siguiendo las líneas ROJAS y VERDES que aparecen. Completa cada línea antes de la siguiente."
Ingredient Icon: (Imagen de pescado)
Cut Type Icon: (Imagen de grilla 3x3)
Panel Color: RGB(0, 100, 200, 255) - Azul
```

**Step 1 - Limón:**
```
Step Name: "Cortar Limón"
Place Instruction Text: "🍋 Coloca el LIMÓN en la tabla de cortar"
Cut Instruction Text: "🔪 Corta por la LÍNEA AMARILLA CURVA siguiendo la forma del limón."
Ingredient Icon: (Imagen de limón)
Cut Type Icon: (Imagen de línea curva)
Panel Color: RGB(200, 200, 0, 255) - Amarillo
```

**Step 2 - Cebolla:**
```
Step Name: "Cortar Cebolla"
Place Instruction Text: "🧅 Coloca un GAJO DE CEBOLLA en la tabla"
Cut Instruction Text: "🔪 Corta siguiendo las LÍNEAS CURVAS CYAN. Completa cada curva."
Ingredient Icon: (Imagen de cebolla)
Cut Type Icon: (Imagen de curvas múltiples)
Panel Color: RGB(150, 0, 150, 255) - Morado
```

**Settings:**
- Hide When Complete: ✓ (si quieres que se oculte al terminar)
- Completion Message Duration: `3` segundos

---

## 🎨 Recursos Visuales Recomendados

### Iconos de Ingredientes:
Crea sprites simples en un programa de dibujo o usa emojis:
- 🐟 Pescado
- 🍋 Limón
- 🧅 Cebolla
- 🌶️ Ají

### Iconos de Tipos de Corte:
- **Grilla**: Dibujo de líneas cruzadas en cuadrícula
- **Línea Curva**: Dibujo de una curva
- **Múltiples Curvas**: Varias curvas paralelas
- **Golpes**: Imagen de cuchillo con líneas de impacto

**NOTA:** Puedes usar TextMeshPro con emojis Unicode si no tienes sprites.

---

## 🔧 Configuración de Controles VR

El sistema usa los controles por defecto de Meta Quest:

**Navegación del Tutorial:**
- **Botón A** (Right Controller): Siguiente página
- **Botón B** (Right Controller): Página anterior

Si quieres cambiar los botones, edita en `TutorialManager`:
```csharp
public OVRInput.Button nextPageButton = OVRInput.Button.One;     // A
public OVRInput.Button previousPageButton = OVRInput.Button.Two; // B
```

---

## ✅ Checklist de Configuración

### TutorialManager:
- [ ] Canvas World Space creado
- [ ] UI creada (Título, Descripción, Botones)
- [ ] Script TutorialManager agregado
- [ ] Referencias de UI conectadas
- [ ] Al menos 3 páginas de tutorial configuradas
- [ ] Botones VR configurados

### InGameInstructionPanel:
- [ ] Canvas World Space creado
- [ ] UI de instrucciones creada
- [ ] Script InGameInstructionPanel agregado
- [ ] Referencia a CuttingBoardSnapZone conectada
- [ ] Instrucciones para cada paso configuradas
- [ ] Iconos de ingredientes asignados
- [ ] Barra de progreso configurada

---

## 🎮 Prueba en VR

### Secuencia de Prueba:

1. **Inicia la escena**
   - Deberías ver el tutorial de bienvenida frente a ti

2. **Navega el tutorial**
   - Presiona A (botón derecho) para avanzar
   - Presiona B para retroceder
   - Lee las 3 páginas

3. **Completa el tutorial**
   - En la última página, presiona A
   - El panel debería ocultarse después de 2 segundos

4. **Instrucciones de juego**
   - El panel de instrucciones debería aparecer
   - Debería decir "Coloca [ingrediente] en la tabla"

5. **Interactúa con las manos**
   - Usa hand tracking (pellizco) para agarrar ingrediente
   - Colócalo en la tabla

6. **Corta el ingrediente**
   - El panel cambia a "Corta siguiendo..."
   - La barra de progreso se llena mientras cortas
   - Al completar, aparece "✅ ¡Paso Completado!"

7. **Siguiente paso**
   - El panel se actualiza al siguiente paso
   - Repite el proceso

---

## 🐛 Solución de Problemas

### El tutorial no aparece:
- Verifica que `TutorialCanvas` esté activo
- Verifica que `Tutorial Pages` tenga al menos 1 elemento
- Revisa la consola de Unity por errores

### Los botones VR no funcionan:
- Verifica que `OVRInput` esté configurado en el proyecto
- Confirma que `navigationController` sea `RTouch`
- Prueba con los controladores físicos

### El panel de instrucciones no se actualiza:
- Verifica que `Cutting Board` esté asignado
- Confirma que el número de `Step Instructions` coincida con `cuttingSteps[]` en `CuttingBoardSnapZone`
- Verifica que los `stepName` coincidan exactamente

### La barra de progreso no funciona:
- Verifica que los ingredientes tengan los componentes de corte:
  - `ProgressiveCutGrid` para pescado
  - `SingleLineCut` para limón
  - `ProgressiveCurveCut` para cebolla
  - `MultiContactCut` para ají
- Confirma que estos scripts estén activados al colocar el ingrediente

---

## 📝 Notas Finales

### Mensajes Clave para el Usuario:

1. **Controladores** = Solo para **movimiento/navegación**
2. **Manos** = Para **agarrar/interactuar**
3. **Instrucciones claras** en cada paso
4. **Feedback visual** con barra de progreso
5. **Colores diferentes** para cada paso (mejor UX)

### Personalización:

Puedes personalizar:
- Textos e instrucciones
- Colores de paneles
- Posición de los Canvas
- Duración de mensajes
- Iconos y sprites
- Botones de navegación

---

¡Listo! Ahora tienes un sistema completo de tutoriales e instrucciones para tu experiencia VR de cocina peruana. 🇵🇪👨‍🍳
