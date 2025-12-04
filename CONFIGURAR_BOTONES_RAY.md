# 🎯 Guía Rápida: Configurar Botones UI para Ray Interactor

## Problema
El rayo se ve (azul/cyan) pero no detecta los botones cuando apuntas a ellos.

## Solución: 3 Pasos Simples

---

### ✅ PASO 1: Agregar Box Collider a los Botones

Los botones UI necesitan un **Box Collider 3D** para que el ray los detecte.

**Para cada botón (Button_Next, Button_Previous):**

1. Selecciona el botón en Hierarchy
2. En Inspector, click en **Add Component**
3. Busca y agrega **Box Collider** (NO "Box Collider 2D")
4. En el Box Collider, configura:
   ```
   ✓ Is Trigger: MARCADO (checked)
   Center: (0, 0, 0)
   Size:
     X: (automático - ancho del botón)
     Y: (automático - alto del botón)
     Z: 10  ← IMPORTANTE: debe ser > 0
   ```

**Por qué Z=10?**
Los botones UI son planos (Z=0). El collider necesita profundidad para que el ray lo detecte.

---

### ✅ PASO 2: Conectar OnClick Events

Los botones necesitan saber QUÉ hacer cuando se hace click.

**Button_Next:**
1. Selecciona `Button_Next` en Hierarchy
2. En Inspector, busca el componente **Button**
3. En la sección **OnClick()**:
   - Click en el **+** (agregar evento)
   - Arrastra el GameObject que tiene **TutorialManager** al campo vacío
   - En el dropdown de la derecha, selecciona:
     ```
     TutorialManager → NextPage()
     ```

**Button_Previous:**
1. Selecciona `Button_Previous` en Hierarchy
2. En Inspector, busca el componente **Button**
3. En la sección **OnClick()**:
   - Click en el **+**
   - Arrastra el GameObject con **TutorialManager**
   - Selecciona:
     ```
     TutorialManager → PreviousPage()
     ```

---

### ✅ PASO 3: Verificar que los Botones sean Interactables

1. Selecciona cada botón (Button_Next, Button_Previous)
2. En el componente **Button**:
   - Verifica que **Interactable** esté ✓ MARCADO
   - Si está desmarcado, el botón se ve pero no responde

---

## 🧪 Probar en Unity

Después de hacer estos cambios:

1. **Presiona Play** en Unity
2. **Mira la consola** (Window → General → Console)
3. **Apunta con el ray a un botón**

   Deberías ver:
   ```
   [RAY] 👉 Apuntando a: 'Button_Next' (Layer: UI)
   ```

4. **Presiona el grip trigger** (botón trasero del controlador)

   Deberías ver:
   ```
   [RAY] 🎮 Botón GripTrigger PRESIONADO en RTouch
   [RAY] 🟢 INTENTANDO CLICK en: Button_Next
   [RAY] ✅ EJECUTANDO CLICK!
   ```

5. El tutorial debería **avanzar a la siguiente página**

---

## 🐛 Solución de Problemas

### El ray no detecta el botón (no cambia de color a amarillo)
**Causa:** No hay Box Collider o el collider es muy pequeño
**Solución:**
- Verifica que el botón tenga un **Box Collider**
- Verifica que **Size Z = 10** (no 0)
- Verifica que **Is Trigger** esté marcado

### El ray detecta el botón (se pone amarillo) pero no hace click
**Causa:** No hay evento OnClick() conectado
**Solución:**
- Abre el componente Button
- En OnClick(), verifica que esté conectado a `TutorialManager.NextPage()`

### El grip trigger no funciona
**Causa:** Controller mal configurado
**Solución:**
- El script ahora **auto-detecta** el controlador correcto
- Verifica en la consola que diga:
  ```
  [RAY] 🔍 Auto-detectado: Mano DERECHA (RTouch) desde 'RightHandAnchor'
  [RAY] 🔍 Auto-detectado: Mano IZQUIERDA (LTouch) desde 'LeftHandAnchor'
  ```

### No veo mensajes de debug en la consola
**Solución:**
- En la consola, asegúrate que el filtro esté en **All** (no solo Errors)
- Verifica que los GameObjects con HandRayInteractor estén activos

---

## 📋 Checklist Final

Antes de probar:
- [ ] Button_Next tiene Box Collider con Z=10
- [ ] Button_Previous tiene Box Collider con Z=10
- [ ] Button_Next OnClick → TutorialManager.NextPage()
- [ ] Button_Previous OnClick → TutorialManager.PreviousPage()
- [ ] Ambos botones tienen Interactable ✓ marcado
- [ ] HandRayInteractor scripts están en GameObjects activos
- [ ] Ray Origin apunta a LeftHandAnchor en un GameObject, RightHandAnchor en el otro

---

¡Listo! Ahora el sistema debería funcionar correctamente. 🎉
