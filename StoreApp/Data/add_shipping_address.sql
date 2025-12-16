-- Migration: Add shipping_address column to orders table
-- Date: 2025-12-14
-- Description: Thêm cột địa chỉ giao hàng (shipping_address) vào bảng orders
--              Cho phép khách hàng thay đổi địa chỉ giao hàng khi đặt đơn

USE storeApp;

-- Kiểm tra và thêm cột nếu chưa tồn tại
SET @col_exists = (
    SELECT COUNT(*) 
    FROM information_schema.COLUMNS 
    WHERE TABLE_SCHEMA = 'storeApp' 
    AND TABLE_NAME = 'orders' 
    AND COLUMN_NAME = 'shipping_address'
);

SET @sql = IF(@col_exists = 0, 
    'ALTER TABLE `orders` ADD COLUMN shipping_address TEXT NULL AFTER note;',
    'SELECT "Column shipping_address already exists" AS message;'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
