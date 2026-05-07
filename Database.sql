USE master
GO

--SP_WHO
--KILL 61

-- Check and drop the database if it exists
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'thuongmaidientudb')
BEGIN
    DROP DATABASE thuongmaidientudb;
END
GO

-- Create the database
CREATE DATABASE thuongmaidientudb;
GO

-- Use the created database
USE thuongmaidientudb;
GO

-- Drop the tables if they exist
IF OBJECT_ID('Order_Items', 'U') IS NOT NULL DROP TABLE Order_Items;
IF OBJECT_ID('Orders', 'U') IS NOT NULL DROP TABLE Orders;
IF OBJECT_ID('Shipments', 'U') IS NOT NULL DROP TABLE Shipments;
IF OBJECT_ID('Cart', 'U') IS NOT NULL DROP TABLE Cart;
IF OBJECT_ID('Wishlist', 'U') IS NOT NULL DROP TABLE Wishlist;
IF OBJECT_ID('Product_Images', 'U') IS NOT NULL DROP TABLE Product_Images;
IF OBJECT_ID('Products', 'U') IS NOT NULL DROP TABLE Products;
IF OBJECT_ID('Categories', 'U') IS NOT NULL DROP TABLE Categories;
IF OBJECT_ID('Users', 'U') IS NOT NULL DROP TABLE Users;
GO

-- Create the Users table
CREATE TABLE Users (
    user_id INT IDENTITY(1,1) PRIMARY KEY,
    first_name NVARCHAR(50) NOT NULL,
    last_name NVARCHAR(50) NOT NULL,
    email VARCHAR(100) NOT NULL,
    password VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
	address NVARCHAR(255),
    role NVARCHAR(50)
);

-- Create the Categories table
CREATE TABLE Categories (
    category_id INT IDENTITY(1,1) PRIMARY KEY,
    category_name NVARCHAR(100) NOT NULL
);

-- Create the Products table
CREATE TABLE Products (
    product_id INT IDENTITY(1,1) PRIMARY KEY,
    category_id INT NOT NULL FOREIGN KEY REFERENCES Categories(category_id),
    product_name NVARCHAR(100) NOT NULL,
    description NVARCHAR(2000),
    price DECIMAL(16, 2),
    discount_price DECIMAL(16, 2),
    stock INT,
    brand NVARCHAR(255),
    is_new BIT
);

-- Create the Product_Images table
CREATE TABLE Product_Images (
    image_id INT IDENTITY(1,1) PRIMARY KEY,
    image_url VARCHAR(255) NOT NULL,
    product_id INT NOT NULL FOREIGN KEY REFERENCES Products(product_id)
);

-- Create the Wishlist table
CREATE TABLE Wishlist (
    wishlist_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL FOREIGN KEY REFERENCES Users(user_id),
    product_id INT NOT NULL FOREIGN KEY REFERENCES Products(product_id)
);

-- Create the Cart table
CREATE TABLE Cart (
    cart_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL FOREIGN KEY REFERENCES Users(user_id),
    product_id INT NOT NULL FOREIGN KEY REFERENCES Products(product_id),
    quantity INT
);

-- Create the Shipments table
CREATE TABLE Shipments (
    shipment_id INT IDENTITY(1,1) PRIMARY KEY,
    user_id INT NOT NULL FOREIGN KEY REFERENCES Users(user_id),
    recipient_first_name NVARCHAR(50) NOT NULL,
    recipient_last_name NVARCHAR(50) NOT NULL,
    recipient_phone VARCHAR(20),
    shipment_address NVARCHAR(500),
    shipment_city NVARCHAR(50),
    shipment_country NVARCHAR(50),
    shipment_zip_code NVARCHAR(20)
);

-- Create the Orders table
CREATE TABLE Orders (
    order_id INT IDENTITY(1,1) PRIMARY KEY,
    shipment_id INT NOT NULL FOREIGN KEY REFERENCES Shipments(shipment_id),
    user_id INT NOT NULL FOREIGN KEY REFERENCES Users(user_id),
    order_date DATETIME,
    total_amount DECIMAL(16, 2),
    status NVARCHAR(50),
    payment_method NVARCHAR(50),
    order_note CHAR(10)
);

-- Create the Order_Items table
CREATE TABLE Order_Items (
    order_item_id INT IDENTITY(1,1) PRIMARY KEY,
    order_id INT NOT NULL FOREIGN KEY REFERENCES Orders(order_id),
    product_id INT NOT NULL FOREIGN KEY REFERENCES Products(product_id),
    quantity INT,
    price DECIMAL(16, 2)
);

CREATE TABLE Product_Specifications (
    spec_id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT,
    spec_group NVARCHAR(100),
    spec_name NVARCHAR(100),
    spec_value NVARCHAR(MAX),
    FOREIGN KEY (product_id) REFERENCES Products(product_id)
);

CREATE TABLE Reviews (
    review_id INT IDENTITY(1,1) PRIMARY KEY,
    product_id INT,
    user_id INT,
    rating INT,
    comment NVARCHAR(1000),
    created_at DATETIME DEFAULT GETDATE(),

    FOREIGN KEY (product_id) REFERENCES Products(product_id),
    FOREIGN KEY (user_id) REFERENCES Users(user_id)
);

--user
INSERT INTO Users (first_name, last_name, email, password, phone, role)
VALUES 
(N'Ha', N'Phạm Viet', 'admin1@gmail.com', '123', '0398857268', 'admin'),
(N'A', N'Nguyen Van', 'admin2@gmail.com', '123', '0987654321', 'admin'),
(N'A', N'Pham Thi', 'cus1@gmail.com', '123', '1122334455', 'customer'),
(N'L', N'Tran Tuyet', 'cus2@gmail.com', '123', '0974871548', 'customer');

-- Insert data into Categories
INSERT INTO Categories (category_name)
VALUES 
(N'Điện thoại'),
(N'Laptop'),
(N'Máy ảnh'),
(N'Phụ kiện');

--dienthoai
INSERT INTO Products (category_id, product_name, description, price, discount_price, stock, brand, is_new)
VALUES 
(1, N'iPhone 17 Pro Max 2TB | Chính hãng', N'iPhone 17 Pro Max sở hữu màn hình Super Retina XDR OLED 6.7 inches với độ phân giải 2796 x 1290 pixels, cung cấp trải nghiệm hình ảnh sắc nét, chân thực. So với các phiên bản tiền nhiệm, thế hệ iPhone 17 bản Pro Max đảm bảo mang tới hiệu năng mạnh mẽ với sự hỗ trợ của chipset Apple A19 Pro, cùng bộ nhớ ấn tượng. Đặc biệt hơn, điện thoại iPhone 17 ProMax mới này còn được đánh giá cao với camera sau 48MP và camera trước 12MP, hỗ trợ chụp ảnh với độ rõ nét cực đỉnh.', 63990000, 62990000, 100, 'Apple', 1),
(1, N'iPhone 16 Pro Max 512GB | Chính hãng VN/A', N'iPhone 16 Pro Max 512GB (VN/A) là mẫu điện thoại cao cấp nhất của Apple trong năm 2024, hiện vẫn là lựa chọn hàng đầu cho người dùng cần màn hình cực đại và dung lượng lưu trữ thoải mái.', 40990000, 39390000, 200, 'Apple', 0),
(1, N'Samsung Galaxy S26 12GB 256GB', N'Samsung Galaxy S26 Base trang bị chip Exynos 2600 tiến trình 2nm, RAM 12GB, bộ nhớ 256GB và pin 4.300mAh nâng cấp. Thiết bị nổi bật với thiết kế 7,2mm mỏng nhẹ, khung Armor Aluminum cải tiến, camera 50MP zoom quang 3x cùng Galaxy AI cá nhân hóa thông minh.', 29990000, 29490000, 150, 'Samsung', 1),
(1, N'Samsung Galaxy S26 Ultra 16GB 1TB', N'Samsung Galaxy S26 Ultra 16GB 1TB sở hữu chip xử lý Snapdragon 8 Elite Gen 5, kết hợp cùng RAM lên đến 16GB cộng thêm dung lượng bộ nhớ trong 1TB. Mẫu Samsung Galaxy này được trang bị màn hình Dynamic AMOLED 2X 6.9 inch. Ngoài ra, điện thoại còn có kiểu dáng đề cao tính di động với trọng lượng 214 gram.', 51890000, 46890000, 150, 'Samsung', 0),
(1, N'Xiaomi 17 Ultra 5G 16GB 1TB', N'Xiaomi 17 Ultra 5G 16GB/1TB vận hành mạnh mẽ trên nền tảng Snapdragon 8 Elite Gen 5 tiên tiến và RAM 16GB xử lý nhiều tác vụ đồng thời một cách trơn tru. Xiaomi 17 Ultra 1TB có không gian lưu trữ rộng cho người dùng thoải mái sử dụng. Camera Leica hiện đại giúp Xiaomi 17 Ultra 16GB 1TB đáp ứng nhu cầu nhiếp ảnh tối ưu.', 42990000, 36990000, 150, 'Xiaomi', 1);

INSERT INTO Product_Images (image_url, product_id)
VALUES 
('apple3_1.png', 1),
('apple3_2.png', 1),
('apple3_3.png', 1),
('apple7_1.png', 2),
('apple7_2.png', 2),
('apple7_3.png', 2),
('samsung3_1.png', 3),
('samsung3_2.png', 3),
('samsung3_3.png', 3),
('samsung4_1.png', 4),
('samsung4_2.png', 4),
('samsung4_3.png', 4),
('xia6_1.png', 5),
('xia6_2.png', 5),
('xia6_3.png', 5);

--Laptop
INSERT INTO Products (category_id, product_name, description, price, discount_price, stock, brand, is_new)
VALUES 
(2, N'MacBook Air M5 15 inch 24GB/1TB Chính Hãng', N'MacBook Air M5 15 inch 24GB/1TB là phiên bản máy tính xách tay cao cấp kết hợp giữa sức mạnh của chip xử lý thế hệ mới và không gian lưu trữ rộng lớn. Thiết bị có màn hình Liquid Retina 15.3 inch sắc nét, bộ nhớ RAM lên đến 24GB giúp xử lý đa nhiệm mượt mà trong một thân máy siêu mỏng nhẹ. Đây là lựa chọn tối ưu cho những người cần hiệu năng duy trì ổn định và tính di động cao để phục vụ công việc dài hạn.', 48590000, 46590000, 100, 'Apple', 0),
(2, N'MacBook Pro 2026 14 inch M5 Max 18‑core CPU | 32‑core GPU 36GB/2TB Chính Hãng', N'Apple MacBook Pro 2026 14" M5 Pro Max 18‑core CPU/32‑core GPU 36GB/2TB (CTY) hiện là tiêu chuẩn vàng cho máy tính trạm di động, mang lại sức mạnh xử lý vượt giới hạn cho các chuyên gia. Dưới đây là những đặc điểm cốt lõi giúp thiết bị này thống trị bảng xếp hạng hiệu năng và đáp ứng kỳ vọng khắt khe nhất của người dùng. ', 99490000, 97490000, 100, 'Apple', 1),
(2, N'MacBook Pro 16 M4 Pro 14CPU 20GPU 24GB 1TB Sạc 140W | Chính hãng Apple Việt Nam ', N'MacBook Pro 16 M4 Pro 14 CPU 20 GPU 24GB 1TB sạc 140W sở hữu cấu hình mạnh mẽ với chip 14 CPU – 20 GPU, RAM 24GB, SSD 1TB đáp ứng tốt các tác vụ chuyên sâu. Vỏ máy gia công từ nhôm nguyên khối bền bỉ với màu Space Black sang trọng, hạn chế bám vân tay. MacBook này là lựa chọn phù hợp cho người dùng sáng tạo, kỹ sư và dân công nghệ.', 81990000, 79990000, 100, 'Apple', 1);

INSERT INTO Product_Images (image_url, product_id)
VALUES 
('mb1_1.png', 6),
('mb1_2.png', 6),
('mb1_3.png', 6),
('mb2_1.png', 7),
('mb2_2.png', 7),
('mb2_3.png', 7),
('mb3_1.png', 8),
('mb3_2.png', 8),
('mb3_3.png', 8);


--Máy ảnh
INSERT INTO Products (category_id, product_name, description, price, discount_price, stock, brand, is_new)
VALUES 
(3, N'Máy ảnh kỹ thuật số Sony ZV1 II', N'Máy ảnh kỹ thuật số Sony ZV-1 II sở hữu ưu điểm ở thiết kế nhỏ gọn giúp bạn có thể mang theo dễ dàng trong những chuyến du lịch hay công tác xa. Hơn nữa, sản phẩm máy ảnh Sony này cũng được trang bị ống kính zoom góc rộng hỗ trợ người dùng căn khung linh hoạt và lưu lại mọi khoảnh khắc vô cùng sắc nét. ', 22990000, 19990000, 100, 'Sony', 1),
(3, N'Máy ảnh kỹ thuật số Sony ZV-1F', N'Máy ảnh kỹ thuật số Sony ZV1F là công cụ hỗ trợ đắc lực cho những ai đang làm vlogger hoặc nhà sáng tạo nội dung trực tuyến. Sở hữu thiết kế nhỏ gọn “bỏ túi” nhưng lại bắt trọn được mọi khung hình với ống kính góc siêu rộng 20 mm. Do vậy, hãy cùng CellphoneS khám phá nhiều hơn về tính năng của máy chụp ảnh Sony thế hệ mới này nhé!', 13990000, 10590000, 100, 'Sony', 0);

INSERT INTO Product_Images (image_url, product_id)
VALUES 
('ma4_1.png', 9),
('ma4_2.png', 9),
('ma4_3.png', 9),
('ma5_1.png', 10),
('ma5_2.png', 10),
('ma5_3.png', 10);

--Phụ kiện
INSERT INTO Products (category_id, product_name, description, price, discount_price, stock, brand, is_new)
VALUES 
(4, N'Tai nghe chụp tai Gaming Sony Inzone H3', N'Sony INZONE H3 được thiết kế với cấu trúc âm học đối xứng, vừa đủ để giúp âm thanh trở nên sống động hơn rất nhiều lần. Nhờ được kích hoạt từ phần mềm PC INZONE Hub, Tai nghe có thể tái tạo tín hiệu thành âm thanh vòm 7.1 kênh.', 10900000, 1000000, 100, 'Sony', 0),
(4, N'Tai nghe Bluetooth Apple AirPods Pro 2 2023 USB-C', N'Airpods Pro 2 Type-C với công nghệ khử tiếng ồn chủ động mang lại khả năng khử ồn lên gấp 2 lần mang lại trải nghiệm nghe - gọi và trải nghiệm âm nhạc ấn tượng. Cùng với đó, điện thoại còn được trang bị công nghệ âm thanh không gian giúp trải nghiệm âm nhạc thêm phần sống động. Airpods Pro 2 Type-C với cổng sạc Type C tiện lợi cùng viên pin mang lại thời gian trải nghiệm lên đến 6 giờ tiện lợi.', 61900000, 5580000, 100, 'Apple', 1);


INSERT INTO Product_Images (image_url, product_id)
VALUES 
('tn1_1.png', 11),
('tn1_2.png', 11),
('tn1_2.png', 11),
('tn2_1.png', 12),
('tn2_2.png', 12),
('tn2_2.png', 12);

-- Insert data into Wishlist
INSERT INTO Wishlist (user_id, product_id)
VALUES 
(1, 1),
(2, 2),
(3, 3),
(4, 4),
(1,10);

 --Insert data into Cart
INSERT INTO Cart (user_id, product_id, quantity)
VALUES 
(1, 1, 2),
(2, 2, 1),
(3, 3, 3),
(4, 10, 2),
(4, 1, 1);


-- Insert data into Shipments
INSERT INTO Shipments (user_id, recipient_first_name, recipient_last_name, recipient_phone, shipment_address, shipment_city, shipment_country, shipment_zip_code)
VALUES 
(1, N'Hà', N'Phạm Việt', N'1234567890', N'123 Bắc Từ Liêm', N'Hà Nội', N'Việt Nam', '12345'),
(2, N'Hà', N'Phạm Việt', N'1234567890', N'456 Nam Từ Liêm', N'Hà Nội', N'Việt Nam', '67890'),
(3, N'Hà', N'Phạm Việt', N'0974871548', N'789 Thanh Xuân', N'Hà Nội', N'Việt Nam', '11223'),
(4, N'Hà', N'Phạm Việt', N'0987654321', N'789 Cầu Giấy', N'Hà Nội', N'Việt Nam', '11229');

-- Insert data into Orders
INSERT INTO Orders (shipment_id, user_id, order_date, total_amount, status, payment_method, order_note)
VALUES 
(1, 1, '2026-04-06 10:00:00', 54780000, 'Shipped', 'Credit Card', 'Note1'),
(2, 2, '2026-04-07 11:00:00', 25390000, 'Pending', 'PayPal', 'Note2'),
(3, 3, '2026-04-08 12:00:00', 88470000, 'Delivered', 'Bank Transfer', 'Note3'),
(4, 3, '2026-04-08 12:00:00', 48570000, 'Shipped', 'Bank Transfer', 'Note4');
-- Insert data into Order_Items
INSERT INTO Order_Items (order_id, product_id, quantity, price)
VALUES 
(1, 1, 2, 62990000),
(2, 2, 1, 39390000),
(3, 3, 3, 29490000),
(4, 10, 2,10590000),
(4, 1, 1, 62990000);

INSERT INTO Product_Specifications (product_id, spec_group, spec_name, spec_value)
VALUES 
(1, N'Màn hình', N'Công nghệ màn hình', N'Super Retina XDR, Công nghệ ProMotion'),
(1, N'Màn hình', N'Màn hình rộng', N'6.3 inch - Tần số quét 120Hz'),
(1, N'Màn hình', N'Độ sáng tối đa', N'3000 nits'),
(1, N'Màn hình', N'Mặt kính cảm ứng', N'Kính cường lực Ceramic Shield 2'),
(1, N'Camera sau', N'Tính năng', N'Zoom quang học 0.5x, 1x, 2x, 4x, 8x, Quay video hiển thị kép'),
(1, N'Camera sau', N'Độ phân giải', N'48MP'),
(1, N'Camera sau', N'Đèn Flash', N'Có'),
(1, N'Camera trước', N'Tính năng', N'Center Stage, Ghi hình kép, Quay video ổn định'),
(1, N'Camera trước', N'Độ phân giải', N'18MP'),
(1, N'Hệ điều hành & CPU', N'Hệ điều hành', N'iOS26'),
(1, N'Hệ điều hành & CPU', N'Chip xử lý (CPU)', N'Chip A17 Pro'),
(1, N'Kết nối', N'Bluetooth', N'v6.0'),
(1, N'Kết nối', N'Cổng kết nối/sạc', N'USB-C'),
(1, N'Kết nối', N'Mạng di động', N'Hỗ trợ 5G'),
(1, N'Kết nối', N'Wifi', N'Wi‑Fi 7'),
(1, N'Pin', N'Dung lượng', N'5000mAh'),
(1, N'Thiết kế', N'Thiết kế', N'Nguyên khối'),
(1, N'Thiết kế', N'Chất liệu', N'Nhôm');