using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Market.Tests.Domain
{
    [TestClass]
    public class ProductTests
    {
        [TestMethod]
        public void Create_ShouldRejectSalePriceLowerThanCost()
        {
            // Arrange
            var cost = 2500m;
            var salePrice = 2000m;

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                Product.Create(
                    "Leche Entera",
                    "Alpina",
                    "Lácteos",
                    UnitType.Volume,
                    1m,
                    cost,
                    salePrice));
        }

        [TestMethod]
        public void Create_ShouldCreateProductWithValidPrice()
        {
            // Arrange
            var cost = 2500m;
            var salePrice = 3200m;

            // Act
            var product = Product.Create(
                "Leche Entera",
                "Alpina",
                "Lácteos",
                UnitType.Volume,
                1m,
                cost,
                salePrice);

            // Assert
            Assert.IsNotNull(product);
            Assert.AreEqual("Leche Entera", product.Name);
            Assert.AreEqual("Alpina", product.Brand);
            Assert.AreEqual(2500m, product.Cost);
            Assert.AreEqual(3200m, product.SalePrice);
            Assert.IsTrue(product.IsActive);
        }

        [TestMethod]
        public void Create_ShouldRejectNegativeCost()
        {
            Assert.Throws<DomainException>(() =>
                Product.Create(
                    "Leche Entera",
                    "Alpina",
                    "Lácteos",
                    UnitType.Volume,
                    1m,
                    -100m,
                    3200m));
        }

        [TestMethod]
        public void Create_ShouldRejectNonPositiveUnitValue()
        {
            Assert.Throws<DomainException>(() =>
                Product.Create(
                    "Leche Entera",
                    "Alpina",
                    "Lácteos",
                    UnitType.Volume,
                    0m,
                    2500m,
                    3200m));
        }

        [TestMethod]
        public void Create_ShouldRejectEmptyName()
        {
            Assert.Throws<DomainException>(() =>
                Product.Create(
                    string.Empty,
                    "Alpina",
                    "Lácteos",
                    UnitType.Volume,
                    1m,
                    2500m,
                    3200m));
        }

        [TestMethod]
        public void ChangePrice_ShouldUpdateSalePrice()
        {
            // Arrange
            var product = Product.Create(
                "Leche Entera",
                "Alpina",
                "Lácteos",
                UnitType.Volume,
                1m,
                2500m,
                3200m);

            // Act
            product.ChangePrice(3500m);

            // Assert
            Assert.AreEqual(3500m, product.SalePrice);
        }

        [TestMethod]
        public void ChangePrice_ShouldRejectNegativePrice()
        {
            // Arrange
            var product = Product.Create(
                "Leche Entera",
                "Alpina",
                "Lácteos",
                UnitType.Volume,
                1m,
                2500m,
                3200m);

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                product.ChangePrice(-1m));
        }

        [TestMethod]
        public void ChangePrice_ShouldUpdateUpdatedAt()
        {
            // Arrange
            var product = Product.Create(
                "Leche Entera",
                "Alpina",
                "Lácteos",
                UnitType.Volume,
                1m,
                2500m,
                3200m);

            // Act
            product.ChangePrice(3500m);

            // Assert
            Assert.IsNotNull(product.UpdatedAt);
        }

        [TestMethod]
        public void ChangePrice_ShouldRejectPriceLowerThanCost()
        {
            // Arrange
            var product = Product.Create(
                "Leche Entera",
                "Alpina",
                "Lácteos",
                UnitType.Volume,
                1m,
                2500m,
                3200m);

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                product.ChangePrice(2000m));

            // Assert
            Assert.AreEqual(3200m, product.SalePrice);
        }

        [TestMethod]
        public void UpdateInformation_ShouldUpdateProductInformation()
        {
            // Arrange
            var product = Product.Create(
                "Leche Entera",
                "Alpina",
                "Lácteos",
                UnitType.Volume,
                1m,
                2500m,
                3200m);

            // Act
            product.UpdateInformation(
                "Leche Deslactosada",
                "Alpina",
                "Lácteos",
                UnitType.Volume,
                1m);

            // Assert
            Assert.AreEqual("Leche Deslactosada", product.Name);
            Assert.AreEqual("Alpina", product.Brand);
            Assert.AreEqual("Lácteos", product.Category);
            Assert.AreEqual(UnitType.Volume, product.UnitType);
            Assert.AreEqual(1m, product.UnitValue);
        }

        [TestMethod]
        public void UpdateInformation_ShouldRejectEmptyName()
        {
            // Arrange
            var product = Product.Create(
                "Leche Entera",
                "Alpina",
                "Lácteos",
                UnitType.Volume,
                1m,
                2500m,
                3200m);

            // Act & Assert
            Assert.ThrowsException<DomainException>(() =>
                product.UpdateInformation(
                    "",
                    "Alpina",
                    "Lácteos",
                    UnitType.Volume,
                    1m));
        }
    }
}
