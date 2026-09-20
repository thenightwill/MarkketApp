using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class SaleItem
    {
        public Guid Id { get; private set; }

        public Guid SaleId { get; private set; }

        public Guid ProductId { get; private set; }

        public decimal Quantity { get; private set; }

        public decimal UnitPrice { get; private set; }

        public decimal Subtotal { get; private set; }

        private SaleItem() { }
    }
}
