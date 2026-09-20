# Seed Data — Supermarket

## 1. Propósito
Datos iniciales para desarrollo, demo y pruebas manuales. Cada registro existe
para ejercitar una regla de negocio concreta (columna "Escenario").

## 2. Reglas de carga
- Solo cuando `Seed:Enabled` es `true` (definido únicamente en `appsettings.Development.json`) y `Database:MigrateOnStartup` es `true`, al arrancar la API.
- Idempotente: si ya existen usuarios, no se vuelve a sembrar.
- Fechas relativas a `UtcNow` (hoy = D), para que los vencimientos sigan siendo válidos.
- Los passwords **no** viven en este documento ni en el código: se leen de
  `appsettings.Development.json` (sección `Seed`, ver §3) y se hashean en Infrastructure.
- Moneda: COP.
- Todo pasa por los métodos de dominio (`Product.Create()`, etc.), nunca por setters.
- Identidad lógica de producto: `Name + Brand + Category + UnitType + UnitValue`.
  El seed no repite ninguna combinación entre productos activos.

## 3. Usuarios
Los passwords se configuran en `appsettings.Development.json` (hay una plantilla en `WebAPI/appsettings.Development.example.json`):

```json
{
  "Seed": {
    "Enabled": true,
    "AdminPassword": "<definir en local>",
    "EmployeePassword": "<definir en local>"
  }
}
```

| Email | Rol | Password | Escenario |
|---|---|---|---|
| admin@supermarket.local | Administrator | `Seed:AdminPassword` | Acceso completo |
| employee1@supermarket.local | Employee | `Seed:EmployeePassword` | Dueño de ventas y tareas |
| employee2@supermarket.local | Employee | `Seed:EmployeePassword` | Verificar que no ve datos de employee1 |

## 4. Productos
| # | Name | Brand | Category | UnitType | UnitValue | Cost | SalePrice | Activo | Escenario |
|---|---|---|---|---|---|---|---|---|---|
| P1 | Leche Entera | Alpina | Lácteos | Volume | 1 | 2500 | 3200 | Sí | FEFO con 3 lotes |
| P2 | Yogurt Fresa | Alpina | Lácteos | Volume | 0.5 | 1800 | 2400 | Sí | Único lote vencido |
| P3 | Arroz Blanco | Diana | Granos | Weight | 1 | 3200 | 4100 | Sí | Stock bajo |
| P4 | Pan Tajado | Bimbo | Panadería | Unit | 1 | 4500 | 6200 | Sí | Stock normal |
| P5 | Gaseosa Cola | Postobón | Bebidas | Volume | 1.5 | 3000 | 3000 | Sí | Borde SalePrice == Cost |
| P6 | Detergente Líquido | Fab | Aseo | Volume | 2 | 9000 | 12500 | Sí | Sin lotes → INSUFFICIENT_STOCK |
| P7 | Huevos AA x30 | Kikes | Proteínas | Unit | 30 | 14000 | 17500 | Sí | Un lote de 3 unidades → prueba de concurrencia |
| P8 | Atún en Agua | Van Camp's | Enlatados | Weight | 0.16 | 4200 | 5600 | **No** | PRODUCT_INACTIVE en venta e inventario |

P8 no tiene lotes: el dominio exige un producto activo para crear lotes, y eso
es intencional.

## 5. Lotes (WarehouseStock)
Fechas relativas a D. Vencido significa `ExpirationDate < D`.

| Producto | BatchNumber | Location | Quantity | MinimumStock | ReceivedDate | ExpirationDate | Escenario |
|---|---|---|---|---|---|---|---|
| P1 | L001 | A-01 | 10 | 5 | D-20 | D+12 | FEFO: se consume primero |
| P1 | L002 | A-01 | 20 | 5 | D-10 | D+42 | FEFO: segundo |
| P1 | L003 | A-02 | 30 | 5 | D-5 | D+72 | FEFO: tercero |
| P2 | YOG-001 | A-03 | 8 | 3 | D-40 | D-5 | INVENTORY_EXPIRED, no vendible |
| P3 | ARR-001 | B-01 | 5 | 10 | D-60 | D+300 | Stock bajo (5 <= 10) |
| P4 | PAN-001 | C-01 | 40 | 10 | D-2 | D+6 | Stock normal, vence pronto |
| P5 | GAS-001 | B-02 | 60 | 12 | D-30 | D+150 | Stock normal |
| P7 | HUE-001 | C-02 | 3 | 2 | D-4 | D+20 | Venta de 3 simultánea → uno recibe INSUFFICIENT_STOCK |

Con estos datos, una solicitud de 15 unidades de P1 consume 10 de L001 y 5 de
L002, igual que el ejemplo FEFO del documento del proyecto.

## 6. Ventas históricas
Dos ventas de `employee1` con `Status = Completed`. El stock inicial de la
sección 5 ya descuenta estas ventas, así que el seed no pasa por el inventario.

| Venta | Fecha | Items | Escenario |
|---|---|---|---|
| S1 | D-7 | P4 x2 a 5900 | Precio histórico (5900) distinto al actual (6200) |
| S2 | D-3 | P1 x4 a 3200 + P5 x1 a 3000 | Venta con varios items, Total = 15 800 |

### Estado de venta
`SaleStatus` es `{ Completed, Incomplete }` (ya implementado en el dominio). No se agregan
más estados para no aumentar la complejidad. El seed solo usa `Completed`;
`Incomplete` es el estado de una venta mientras se arma en el dominio: nace así y pasa
a `Completed` con `Complete()`. `CreateSale` solo persiste ventas `Completed`.

## 7. Tareas
| Usuario | Title | Status | DueDate | Escenario |
|---|---|---|---|---|
| employee1 | Revisar lotes por vencer | Pending | D+2 | Transición válida → InProgress |
| employee1 | Reponer arroz | InProgress | D+5 | Transición válida → Completed |
| employee1 | Conteo de bodega A | Completed | D-1 | Estado final, sin transiciones |
| employee2 | Etiquetar lácteos | Pending | D+3 | Aislamiento: employee1 recibe 404/403 |

## 8. Casos de prueba que cubre el seed
- FEFO multi-lote (P1) y stock bajo (P3).
- Lote vencido (P2) y producto inactivo (P8).
- Producto sin stock (P6) y concurrencia sobre P7.
- Borde `SalePrice == Cost` (P5).
- Precio histórico en `SaleItem` (S1).
- Aislamiento de tareas entre usuarios.
- Transiciones inválidas: Pending → Completed o Completed → cualquiera → `INVALID_TASK_TRANSITION`.
