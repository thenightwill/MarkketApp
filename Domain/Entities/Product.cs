using Domain.Enums;
using Domain.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Product
    {
        public Guid Id { get; private set; }

        public string Name { get; private set; }

        public string Brand { get; private set; }

        public string Category { get; private set; }

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

        public static Product Create(
            string name,
            string brand,
            string category,
            UnitType unitType,
            decimal unitValue,
            decimal cost,
            decimal salePrice)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new DomainException("Product name is required.");

            if (string.IsNullOrWhiteSpace(brand))
                throw new DomainException("Product brand is required.");

            if (string.IsNullOrWhiteSpace(category))
                throw new DomainException("Product category is required.");

            if (unitValue <= 0)
                throw new DomainException("Unit value must be greater than zero.");

            if (cost < 0)
                throw new DomainException("Cost cannot be negative.");

            if (salePrice < cost)
                throw new DomainException(
                    "Sale price cannot be lower than cost.");

            return new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                Brand = brand,
                Category = category,
                UnitType = unitType,
                UnitValue = unitValue,
                Cost = cost,
                SalePrice = salePrice,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
        public void ChangePrice(decimal newSalePrice)
        {
            if (newSalePrice < Cost)
            {
                throw new DomainException(
                    "Sale price cannot be lower than cost.");
            }

            SalePrice = newSalePrice;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        public void UpdateInformation(
                                        string name,
                                        string brand,
                                        string category,
                                        UnitType unitType,
                                        decimal unitValue)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new DomainException(
                    "Product name is required.");
            }

            if (string.IsNullOrWhiteSpace(brand))
            {
                throw new DomainException(
                    "Product brand is required.");
            }

            if (string.IsNullOrWhiteSpace(category))
            {
                throw new DomainException(
                    "Product category is required.");
            }

            if (unitValue <= 0)
            {
                throw new DomainException(
                    "Unit value must be greater than zero.");
            }

            Name = name;
            Brand = brand;
            Category = category;
            UnitType = unitType;
            UnitValue = unitValue;

            UpdatedAt = DateTimeOffset.UtcNow;
        }


    }
}
