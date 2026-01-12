namespace Infrastructure.Persistence.Config;

public static class DataSchemaConstants
{
    public static class Employee
    {
        public const int NAME_LENGTH = 100;
        public const int POSITION_LENGTH = 100;
        public const int STATUS_LENGTH = 20;
    }

    public static class Mission
    {
        public const int NAME_LENGTH = 225;
        public const int DESCRIPTION_LENGTH = 1000;
        public const int CATEGORY_LENGTH = 225;
        public const int STATUS_LENGTH = 20;
    }
}
