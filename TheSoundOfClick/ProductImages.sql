CREATE TABLE IF NOT EXISTS product_images (
    product_id uuid PRIMARY KEY REFERENCES products(id) ON DELETE CASCADE,
    image_path text NOT NULL
);
