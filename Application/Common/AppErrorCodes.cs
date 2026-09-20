namespace Application.Common;

public static class AppErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string ProductNotFound = "PRODUCT_NOT_FOUND";
    public const string ProductInactive = "PRODUCT_INACTIVE";
    public const string DuplicateProduct = "DUPLICATE_PRODUCT";
    public const string InventoryNotFound = "INVENTORY_NOT_FOUND";
    public const string DuplicateBatch = "DUPLICATE_BATCH";
    public const string SaleNotFound = "SALE_NOT_FOUND";
    public const string TaskNotFound = "TASK_NOT_FOUND";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
}
