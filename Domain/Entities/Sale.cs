using Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    public class Sale
    {
        public Guid Id { get; private set; }

        public Guid UserId { get; private set; }

        public DateTime SaleDate { get; private set; }

        public decimal Total { get; private set; }

        public SaleStatus Status { get; private set; }

        private readonly List<SaleItem> _items = new();

        public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

        private Sale() { }
    }
}
