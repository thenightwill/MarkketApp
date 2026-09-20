using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class WarehouseStock
    {
        public Guid Id { get; private set; }

        public Guid ProductId { get; private set; }

        public string BatchNumber { get; private set; }

        public string Location { get; private set; }

        public decimal Quantity { get; private set; }

        public decimal MinimumStock { get; private set; }

        public DateTime ReceivedDate { get; private set; }

        public DateTime ExpirationDate { get; private set; }

        public bool IsActive { get; private set; }

        private WarehouseStock() { }
    }
}
