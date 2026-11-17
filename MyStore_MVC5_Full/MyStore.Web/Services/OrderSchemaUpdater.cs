using System;
using System.Configuration;
using System.Data.SqlClient;

namespace MyStore.Web.Services
{
    /// <summary>
    /// Safely adds the ShippingFee column to an existing MyStore database.
    /// Does not delete tables, orders, or product data. Runs at most once per process.
    /// Requires ALTER TABLE permission on dbo.[Order] for the Windows account running IIS Express.
    /// </summary>
    public static class OrderSchemaUpdater
    {
        private static readonly object Sync = new object();
        private static bool ready;

        public static void EnsureShippingFee()
        {
            if (ready) return;
            lock (Sync)
            {
                if (ready) return;
                var cs = ConfigurationManager.ConnectionStrings["MyStoreEntities"];
                if (cs == null || string.IsNullOrWhiteSpace(cs.ConnectionString))
                    throw new InvalidOperationException("Thiếu kết nối MyStoreEntities trong Web.config.");
                try
                {
                    using (var connection = new SqlConnection(cs.ConnectionString))
                    {
                        connection.Open();
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandTimeout = 60;
                            command.CommandText = @"
IF OBJECT_ID(N'dbo.[Order]', N'U') IS NULL
BEGIN
    RAISERROR(N'Không tìm thấy bảng dbo.[Order] trong MyStore.', 16, 1);
END
ELSE IF COL_LENGTH(N'dbo.[Order]', N'ShippingFee') IS NULL
BEGIN
    EXEC(N'ALTER TABLE dbo.[Order]
          ADD ShippingFee DECIMAL(18,2) NOT NULL
          CONSTRAINT DF_Order_ShippingFee_Auto DEFAULT(0) WITH VALUES');
END";
                            command.ExecuteNonQuery();
                            command.CommandText = @"SELECT CASE WHEN COL_LENGTH(N'dbo.[Order]', N'ShippingFee') IS NOT NULL
                                                                THEN 1 ELSE 0 END";
                            if (Convert.ToInt32(command.ExecuteScalar()) != 1)
                                throw new InvalidOperationException("Không thể kiểm tra cột ShippingFee trong MyStore.");
                            ready = true;
                        }
                    }
                }
                catch (SqlException ex)
                {
                    throw new InvalidOperationException(
                        "Chưa thể tự cập nhật cột ShippingFee trong database MyStore. " +
                        "Hãy kiểm tra SQL Server đang chạy và quyền ALTER TABLE của tài khoản Windows. " +
                        "Có thể chạy CAPNHAT_DONHANG_1_LAN.cmd mà không cần mở SSMS. " +
                        "Thông báo SQL: " + ex.Message, ex);
                }
            }
        }
    }
}
