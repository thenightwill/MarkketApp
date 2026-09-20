using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Market.Tests.Domain;

[TestClass]
public class ProductTests
{
    private static Product CreateValidProduct() =>
        Product.Create("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m);

    [TestMethod]
    public void Create_ShouldRejectSalePriceLowerThanCost()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Product.Create("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 2000m));

        Assert.AreEqual(DomainErrorCodes.InvalidProductPrice, ex.Code);
    }

    [TestMethod]
    public void Create_ShouldCreateProductWithValidData()
    {
        var product = CreateValidProduct();

        Assert.AreNotEqual(Guid.Empty, product.Id);
        Assert.AreEqual("Leche Entera", product.Name);
        Assert.AreEqual("Alpina", product.Brand);
        Assert.AreEqual("Lácteos", product.Category);
        Assert.AreEqual(UnitType.Volume, product.UnitType);
        Assert.AreEqual(1m, product.UnitValue);
        Assert.AreEqual(2500m, product.Cost);
        Assert.AreEqual(3200m, product.SalePrice);
        Assert.IsTrue(product.IsActive);
        Assert.IsNull(product.UpdatedAt);
    }

    [TestMethod]
    public void Create_ShouldAcceptSalePriceEqualToCost()
    {
        var product = Product.Create("Gaseosa Cola", "Postobón", "Bebidas", UnitType.Volume, 1.5m, 3000m, 3000m);

        Assert.AreEqual(3000m, product.SalePrice);
    }

    [TestMethod]
    public void Create_ShouldTrimTextFields()
    {
        var product = Product.Create("  Leche Entera ", " Alpina ", " Lácteos ", UnitType.Volume, 1m, 2500m, 3200m);

        Assert.AreEqual("Leche Entera", product.Name);
        Assert.AreEqual("Alpina", product.Brand);
        Assert.AreEqual("Lácteos", product.Category);
    }

    [TestMethod]
    public void Create_ShouldRejectNegativeCost()
    {
        Assert.Throws<DomainException>(() =>
            Product.Create("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, -100m, 3200m));
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyName()
    {
        Assert.Throws<DomainException>(() =>
            Product.Create(string.Empty, "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m));
    }

    [TestMethod]
    public void Create_ShouldRejectWhitespaceName()
    {
        Assert.Throws<DomainException>(() =>
            Product.Create("   ", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m));
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyBrand()
    {
        Assert.Throws<DomainException>(() =>
            Product.Create("Leche Entera", string.Empty, "Lácteos", UnitType.Volume, 1m, 2500m, 3200m));
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyCategory()
    {
        Assert.Throws<DomainException>(() =>
            Product.Create("Leche Entera", "Alpina", string.Empty, UnitType.Volume, 1m, 2500m, 3200m));
    }

    [TestMethod]
    public void Create_ShouldRejectZeroUnitValue()
    {
        Assert.Throws<DomainException>(() =>
            Product.Create("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 0m, 2500m, 3200m));
    }

    [TestMethod]
    public void Create_ShouldRejectNegativeUnitValue()
    {
        Assert.Throws<DomainException>(() =>
            Product.Create("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, -1m, 2500m, 3200m));
    }

    [TestMethod]
    public void ChangePrice_ShouldUpdateSalePrice()
    {
        var product = CreateValidProduct();

        product.ChangePrice(3500m);

        Assert.AreEqual(3500m, product.SalePrice);
    }

    [TestMethod]
    public void ChangePrice_ShouldRejectPriceLowerThanCost()
    {
        var product = CreateValidProduct();

        Assert.Throws<DomainException>(() => product.ChangePrice(2000m));

        Assert.AreEqual(3200m, product.SalePrice);
        Assert.IsNull(product.UpdatedAt);
    }

    [TestMethod]
    public void ChangePrice_ShouldRejectNegativePrice()
    {
        var product = CreateValidProduct();

        Assert.Throws<DomainException>(() => product.ChangePrice(-1m));
    }

    [TestMethod]
    public void ChangePrice_ShouldUpdateUpdatedAt()
    {
        var product = CreateValidProduct();

        product.ChangePrice(3500m);

        Assert.IsNotNull(product.UpdatedAt);
    }

    [TestMethod]
    public void ChangePricing_ShouldUpdateCostAndSalePrice()
    {
        var product = CreateValidProduct();

        product.ChangePricing(4000m, 5000m);

        Assert.AreEqual(4000m, product.Cost);
        Assert.AreEqual(5000m, product.SalePrice);
        Assert.IsNotNull(product.UpdatedAt);
    }

    [TestMethod]
    public void ChangePricing_ShouldNotModifyProductWhenPriceIsLowerThanCost()
    {
        var product = CreateValidProduct();

        Assert.Throws<DomainException>(() => product.ChangePricing(4000m, 3000m));

        Assert.AreEqual(2500m, product.Cost);
        Assert.AreEqual(3200m, product.SalePrice);
    }

    [TestMethod]
    public void ChangePricing_ShouldRejectNegativeCost()
    {
        var product = CreateValidProduct();

        Assert.Throws<DomainException>(() => product.ChangePricing(-1m, 3000m));
    }

    [TestMethod]
    public void UpdateInformation_ShouldUpdateProductInformation()
    {
        var product = CreateValidProduct();

        product.UpdateInformation("Leche Deslactosada", "Colanta", "Lácteos y derivados", UnitType.Weight, 2m);

        Assert.AreEqual("Leche Deslactosada", product.Name);
        Assert.AreEqual("Colanta", product.Brand);
        Assert.AreEqual("Lácteos y derivados", product.Category);
        Assert.AreEqual(UnitType.Weight, product.UnitType);
        Assert.AreEqual(2m, product.UnitValue);
    }

    [TestMethod]
    public void UpdateInformation_ShouldRejectEmptyName()
    {
        var product = CreateValidProduct();

        Assert.Throws<DomainException>(() =>
            product.UpdateInformation(string.Empty, "Alpina", "Lácteos", UnitType.Volume, 1m));
    }

    [TestMethod]
    public void UpdateInformation_ShouldNotModifyProductWhenDataIsInvalid()
    {
        var product = CreateValidProduct();

        Assert.Throws<DomainException>(() =>
            product.UpdateInformation("Leche Deslactosada", "Colanta", "Lácteos", UnitType.Weight, 0m));

        Assert.AreEqual("Leche Entera", product.Name);
        Assert.AreEqual("Alpina", product.Brand);
        Assert.AreEqual("Lácteos", product.Category);
        Assert.AreEqual(UnitType.Volume, product.UnitType);
        Assert.AreEqual(1m, product.UnitValue);
        Assert.IsNull(product.UpdatedAt);
    }

    [TestMethod]
    public void UpdateInformation_ShouldUpdateUpdatedAt()
    {
        var product = CreateValidProduct();

        product.UpdateInformation("Leche Deslactosada", "Alpina", "Lácteos", UnitType.Volume, 1m);

        Assert.IsNotNull(product.UpdatedAt);
    }

    [TestMethod]
    public void Deactivate_ShouldSetProductAsInactive()
    {
        var product = CreateValidProduct();

        product.Deactivate();

        Assert.IsFalse(product.IsActive);
        Assert.IsNotNull(product.UpdatedAt);
    }

    [TestMethod]
    public void Activate_ShouldSetProductAsActive()
    {
        var product = CreateValidProduct();
        product.Deactivate();

        product.Activate();

        Assert.IsTrue(product.IsActive);
    }

    [TestMethod]
    public void IsValidQuantity_ShouldRequirePositiveValues()
    {
        var product = CreateValidProduct();

        Assert.IsTrue(product.IsValidQuantity(1.5m));
        Assert.IsFalse(product.IsValidQuantity(0m));
        Assert.IsFalse(product.IsValidQuantity(-1m));
    }

    [TestMethod]
    public void IsValidQuantity_ShouldRequireWholeNumbersForUnitProducts()
    {
        var product = Product.Create("Pan Tajado", "Bimbo", "Panadería", UnitType.Unit, 1m, 4500m, 6200m);

        Assert.IsTrue(product.IsValidQuantity(3m));
        Assert.IsFalse(product.IsValidQuantity(1.5m));
    }

    [TestMethod]
    public void Update_ShouldChangeInformationAndPricingTogether()
    {
        var product = CreateValidProduct();

        product.Update("Leche Deslactosada", "Colanta", "Lácteos", UnitType.Volume, 2m, 3000m, 4000m);

        Assert.AreEqual("Leche Deslactosada", product.Name);
        Assert.AreEqual(2m, product.UnitValue);
        Assert.AreEqual(3000m, product.Cost);
        Assert.AreEqual(4000m, product.SalePrice);
        Assert.IsNotNull(product.UpdatedAt);
    }

    [TestMethod]
    public void Update_ShouldNotModifyProductWhenPricingIsInvalid()
    {
        var product = CreateValidProduct();

        Assert.Throws<DomainException>(() =>
            product.Update("Leche Deslactosada", "Colanta", "Lácteos", UnitType.Volume, 2m, 5000m, 4000m));

        Assert.AreEqual("Leche Entera", product.Name);
        Assert.AreEqual(1m, product.UnitValue);
        Assert.AreEqual(2500m, product.Cost);
        Assert.AreEqual(3200m, product.SalePrice);
        Assert.IsNull(product.UpdatedAt);
    }

    [TestMethod]
    public void Update_ShouldNotModifyProductWhenInformationIsInvalid()
    {
        var product = CreateValidProduct();

        Assert.Throws<DomainException>(() =>
            product.Update(" ", "Colanta", "Lácteos", UnitType.Volume, 2m, 3000m, 4000m));

        Assert.AreEqual(2500m, product.Cost);
        Assert.AreEqual("Leche Entera", product.Name);
    }
}
