namespace Domain.Exceptions;

public static class DomainErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string InvalidProduct = "INVALID_PRODUCT";
    public const string InvalidProductPrice = "INVALID_PRODUCT_PRICE";
    public const string InvalidInventory = "INVALID_INVENTORY";
    public const string InvalidQuantity = "INVALID_QUANTITY";
    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string InventoryExpired = "INVENTORY_EXPIRED";
    public const string InvalidSale = "INVALID_SALE";
    public const string SaleEmpty = "SALE_EMPTY";
    public const string SaleAlreadyCompleted = "SALE_ALREADY_COMPLETED";
    public const string DuplicateSaleItem = "DUPLICATE_SALE_ITEM";
    public const string InvalidTask = "INVALID_TASK";
    public const string InvalidTaskTransition = "INVALID_TASK_TRANSITION";
    public const string InvalidUser = "INVALID_USER";
}
