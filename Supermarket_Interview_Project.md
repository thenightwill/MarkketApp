# Supermarket --- Technical Interview Project

## 1. Context

Proyecto para una entrevista técnica .NET/Angular basado en el ejercicio
de entrevista proporcionado.

Objetivo: construir una aplicación web con API y capa de datos usando
.NET/C#, aplicando Clean Architecture y TDD. La solución debe incluir
CRUD, autenticación/autorización, frontend, pruebas, datos semilla,
README y una demostración del uso crítico de GenAI.

## 2. Alcance funcional

### Productos

CRUD de productos para administrar el catálogo de productos vendidos.

Atributos principales: - Name - Brand - Category - UnitType -
UnitValue - Cost - SalePrice - IsActive - CreatedAt - UpdatedAt

Reglas: - Name requerido. - Brand requerida. - Category requerida. -
UnitValue \> 0. - Cost \>= 0. - SalePrice \>= Cost. - No duplicar
productos activos según la identidad lógica definida. - Los productos se
desactivan lógicamente en lugar de eliminarse físicamente.

### Inventario / Bodega

CRUD para administrar lotes de productos.

Atributos: - ProductId - BatchNumber - Location - Quantity -
MinimumStock - ReceivedDate - ExpirationDate - IsActive - CreatedAt

Reglas: - El producto debe existir y estar activo. - Quantity \> 0 al
registrar un lote. - MinimumStock \>= 0. - ExpirationDate \>
ReceivedDate. - BatchNumber no debe duplicarse para el mismo producto. -
No permitir stock negativo. - Un producto vencido no debe utilizarse en
una venta. - El stock bajo puede determinarse dinámicamente mediante
Quantity \<= MinimumStock.

### Ventas

El sistema tendrá un módulo de ventas.

Una venta contiene: - UserId - SaleDate - Total - Status - SaleItems

Cada SaleItem contiene: - ProductId - Quantity - UnitPrice - Subtotal

Reglas: - Una venta debe contener al menos un item. - Quantity \> 0. -
El producto debe existir y estar activo. - No se puede vender más stock
disponible. - Se utiliza FEFO (First Expired, First Out) para
seleccionar lotes. - El precio utilizado en una venta se obtiene del
backend. - SaleItem conserva el precio histórico de la operación. -
Venta e inventario deben actualizarse de forma transaccional. - Debe
considerarse concurrencia para evitar sobreventa.

### Tasks

CRUD de tareas para cumplir el requerimiento de GenAI.

Atributos: - UserId - Title - Description - Status - DueDate -
CreatedAt - UpdatedAt

Estados: - Pending - InProgress - Completed

Transiciones: - Pending -\> InProgress - InProgress -\> Completed

Las tareas pertenecen a un usuario y el usuario no debe acceder a tareas
de otros usuarios.

### Usuarios y autenticación

-   Registro.
-   Login.
-   JWT Bearer.
-   Password almacenado mediante hash.
-   Employee y Administrator.
-   UserId se obtiene del JWT, no del request para operaciones
    autenticadas.

## 3. Modelo de dominio

Entidades: - User - Product - WarehouseStock - Sale - SaleItem - Task

Aggregates: - Product Aggregate: Product como root. - Inventory
Aggregate: WarehouseStock como root. - Sale Aggregate: Sale como root;
SaleItem pertenece a Sale. - Task Aggregate: Task como root. - User
Aggregate: User como root.

Relaciones principales:

User 1:N Sale User 1:N Task Product 1:N WarehouseStock Sale 1:N SaleItem
SaleItem N:1 Product

## 4. Responsabilidades de dominio

### Product

Comportamientos: - Create() - ChangePrice() - UpdateInformation() -
Activate() - Deactivate()

El dominio debe proteger: - SalePrice \>= Cost. - Cost \>= 0. -
UnitValue \> 0. - Campos obligatorios. - Un cambio inválido no debe
dejar el objeto parcialmente modificado.

### WarehouseStock

Comportamientos previstos: - AddQuantity() - RemoveQuantity() -
IsExpired() - IsLowStock()

### Sale

Comportamientos previstos: - AddItem() - CalculateTotal() - Complete()

### SaleItem

Responsable de mantener: - Quantity \> 0. - UnitPrice \>= 0. - Subtotal
= Quantity \* UnitPrice.

### Task

Responsable de proteger las transiciones de estado.

## 5. Clean Architecture

Solution:

``` text
Supermarket.sln
|
+-- src
|   +-- Supermarket.Api
|   +-- Supermarket.Application
|   +-- Supermarket.Domain
|   +-- Supermarket.Infrastructure
|
+-- tests
    +-- Supermarket.Domain.Tests
    +-- Supermarket.Application.Tests
    +-- Supermarket.Infrastructure.Tests
    +-- Supermarket.Api.Tests
```

### Domain

No depende de API, EF Core, SQL Server o infraestructura.

Contendrá: - Entities - Enums - Domain Exceptions

### Application

Contendrá: - Use Cases - DTOs - Interfaces - Orquestación de procesos

Ejemplos: - CreateProduct - UpdateProduct - CreateStock - CreateSale -
CreateTask - Login

### Infrastructure

Contendrá: - EF Core - DbContext - Entity configurations -
Repositories - JWT - Password hashing - Migrations

### API

Contendrá: - Controllers - Middleware - Dependency Injection - HTTP
configuration

Los controllers deben ser delgados y delegar la lógica al Application.

## 6. CreateSale

Flujo:

``` text
Angular
  |
  v
SalesController
  |
  v
CreateSaleUseCase
  |
  +--> ProductRepository
  |
  +--> InventoryRepository
  |
  +--> FEFO allocation
  |
  +--> Create Sale
  |
  +--> Create SaleItems
  |
  +--> Remove stock
  |
  +--> Commit transaction
  |
  v
Response
```

Request:

``` json
{
  "items": [
    {
      "productId": "guid",
      "quantity": 5
    }
  ]
}
```

El request no contiene: - UserId - UnitPrice - BatchNumber

El backend determina esos valores.

### FEFO

Ejemplo:

``` text
L001 -> 10 unidades -> vence Oct 1
L002 -> 20 unidades -> vence Nov 1
L003 -> 30 unidades -> vence Dec 1

Solicitud: 15 unidades

L001 -> consumir 10
L002 -> consumir 5
```

### Transacción

La creación de Sale, SaleItems y disminución del inventario deben ser
consistentes.

Conceptualmente:

``` text
BEGIN TRANSACTION
    INSERT Sale
    INSERT SaleItems
    UPDATE WarehouseStock
COMMIT
```

Si ocurre un error:

``` text
ROLLBACK
```

Debe considerarse optimistic concurrency para evitar que dos ventas
simultáneas sobrevendan el mismo stock.

## 7. API Contract

### Authentication

``` text
POST /api/auth/register
POST /api/auth/login
```

### Products

``` text
GET    /api/products
GET    /api/products/{id}
POST   /api/products
PUT    /api/products/{id}
DELETE /api/products/{id}
```

DELETE representa desactivación lógica.

### Inventory

``` text
GET /api/inventory
GET /api/inventory/{id}
POST /api/inventory
PUT /api/inventory/{id}
```

La cantidad no debería modificarse arbitrariamente mediante CRUD; los
movimientos de cantidad representan operaciones de negocio.

### Sales

``` text
POST /api/sales
GET  /api/sales
GET  /api/sales/{id}
```

### Tasks

``` text
GET    /api/tasks
GET    /api/tasks/{id}
POST   /api/tasks
PUT    /api/tasks/{id}
DELETE /api/tasks/{id}
```

## 8. HTTP Status Codes

``` text
201 Created
200 OK
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
```

Errores de dominio/API pueden utilizar códigos como:

``` text
PRODUCT_NOT_FOUND
PRODUCT_INACTIVE
DUPLICATE_PRODUCT
INVALID_PRODUCT_PRICE
INVENTORY_NOT_FOUND
INVENTORY_EXPIRED
INSUFFICIENT_STOCK
SALE_EMPTY
INVALID_QUANTITY
TASK_NOT_FOUND
INVALID_TASK_TRANSITION
INVALID_CREDENTIALS
EMAIL_ALREADY_EXISTS
```

## 9. TDD con MSTest

Framework: - MSTest.TestFramework - MSTest.TestAdapter

Estructura:

``` text
Supermarket.Domain.Tests
|
+-- ProductTests.cs
+-- WarehouseStockTests.cs
+-- SaleTests.cs
+-- SaleItemTests.cs
+-- TaskTests.cs
```

Proceso:

``` text
RED
  ->
GREEN
  ->
REFACTOR
```

### Product --- tests ya definidos

-   Create_ShouldRejectSalePriceLowerThanCost
-   Create_ShouldCreateProductWithValidData
-   Create_ShouldRejectNegativeCost
-   Create_ShouldRejectEmptyName
-   Create_ShouldRejectWhitespaceName
-   Create_ShouldRejectEmptyBrand
-   Create_ShouldRejectEmptyCategory
-   Create_ShouldRejectZeroUnitValue
-   Create_ShouldRejectNegativeUnitValue
-   ChangePrice_ShouldUpdateSalePrice
-   ChangePrice_ShouldRejectPriceLowerThanCost
-   ChangePrice_ShouldUpdateUpdatedAt
-   UpdateInformation_ShouldUpdateProductInformation
-   UpdateInformation_ShouldRejectEmptyName
-   UpdateInformation_ShouldNotModifyProductWhenDataIsInvalid
-   UpdateInformation_ShouldUpdateUpdatedAt
-   Deactivate_ShouldSetProductAsInactive

Ejemplo MSTest:

``` csharp
[TestMethod]
public void Create_ShouldRejectSalePriceLowerThanCost()
{
    const decimal cost = 2500m;
    const decimal salePrice = 2000m;

    Assert.ThrowsException<DomainException>(() =>
        Product.Create(
            "Leche Entera",
            "Alpina",
            "Lácteos",
            UnitType.Volume,
            1m,
            cost,
            salePrice));
}
```

## 10. Product actual

La entidad debe utilizar encapsulación:

``` csharp
public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Brand { get; private set; } = null!;
    public string Category { get; private set; } = null!;
    public UnitType UnitType { get; private set; }
    public decimal UnitValue { get; private set; }
    public decimal Cost { get; private set; }
    public decimal SalePrice { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Product()
    {
    }
}
```

La creación debe pasar por `Product.Create()` y los cambios importantes
por métodos de dominio, evitando setters públicos.

## 11. Angular

Estructura prevista:

``` text
supermarket-web
|
+-- core
|   +-- auth
|   +-- interceptors
|   +-- guards
|
+-- shared
|   +-- components
|
+-- features
    +-- auth
    +-- products
    +-- inventory
    +-- sales
    +-- tasks
```

Tecnologías: - Angular moderno - Standalone Components - Signals cuando
aporten valor - RxJS - Reactive Forms - HttpClient - Route Guards - HTTP
Interceptor

Angular no debe decidir: - Precio definitivo. - Lote utilizado. -
FEFO. - Reglas de inventario.

Esas reglas permanecen en el backend.

## 12. Decisiones deliberadamente evitadas

No introducir inicialmente: - Microservices - CQRS + MediatR - Event
Sourcing - Domain Events complejos - Redis - Message Broker - Outbox
Pattern - Kubernetes

La razón es evitar sobrearquitectura. Se pueden evaluar posteriormente
si aparece una necesidad real.

## 13. Próximo paso

Terminar `Product` con: - `Deactivate()` - `Activate()`

Después pasar a:

``` text
WarehouseStock
  |
  +-- IsExpired()
  +-- IsLowStock()
  +-- AddQuantity()
  +-- RemoveQuantity()
```

Y continuar con TDD hasta llegar a:

``` text
Sale
  |
  +-- SaleItem
  +-- CalculateTotal()
  +-- FEFO
  +-- CreateSaleUseCase
```

La prioridad es mantener la implementación alineada con: - Clean
Architecture. - TDD. - Separación de responsabilidades. - Reglas de
negocio protegidas por el dominio. - API delgada. - Infrastructure
aislada. - Código explicable durante el code review.
