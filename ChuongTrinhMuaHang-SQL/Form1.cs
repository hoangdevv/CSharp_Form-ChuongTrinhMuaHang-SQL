using ChuongTrinhMuaHang_SQL.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChuongTrinhMuaHang_SQL
{
    public partial class Form1 : Form
    {

        ModelProducts modelProducts = new ModelProducts();

        public Form1()
        {
            InitializeComponent();
        }
        
        void updateTongTien()
        {

            decimal donGia = 0;
            if (cbbSanPham.SelectedIndex == 0)
            {
                txtDonGia.Text = "2000000";
                donGia = 2000000;
            }
            if (cbbSanPham.SelectedIndex == 1)
            {
                txtDonGia.Text = "20000";
                donGia = 20000;
            }
            if (cbbSanPham.SelectedIndex == 2)
            {
                txtDonGia.Text = "120000";
                donGia = 120000;
            }
            if (int.TryParse(txtSoLuong.Text, out int soLuong))
            {
                decimal tongTien = donGia * soLuong;
                txtTongTien.Text = tongTien.ToString();
            }
        }
        void UpdateTotalAmount()
        {
            decimal tongTien = 0;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                decimal giaTriTongTien = 0;
                if (decimal.TryParse(row.Cells[4].Value.ToString(), out giaTriTongTien))
                {
                    tongTien += giaTriTongTien;
                }
            }

            txtTongTienHoaDon.Text = tongTien.ToString();
        }

        bool KTInput()
        {
            if(!int.TryParse(txtSoLuong.Text,out int result))
            {
                MessageBox.Show("Vui lòng nhập đúng định dạng số lương!", "Cảnh báo", MessageBoxButtons.OK);
                return false;
            }
            if(txtSoLuong.Text == "")
            {
                MessageBox.Show("Vui lòng nhập số lượng!", "Cảnh báo", MessageBoxButtons.OK);
                return false;
            }
            
            return true;
        }
        bool productExistsInGrid = false;
       
        void Clear_Input()
        {
            txtGhiChu.Clear();
            cbbSanPham.SelectedIndex = -1;
            txtSoLuong.Clear();
            txtDonGia.Clear();
            txtTongTien.Clear();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            List<Product> listProducts = modelProducts.Products.ToList();

            cbbSanPham.DataSource = listProducts;
            cbbSanPham.DisplayMember = "ProductName";
            cbbSanPham.ValueMember = "ProductId";
            txtSoLuong.Text = "1";
            // cbbSanPham.SelectedIndex = 0;
            txtTongTienHoaDon.Text = "0";

        }

        private void cbbSanPham_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateTongTien();
        }
        private void txtSoLuong_TextChanged_1(object sender, EventArgs e)
        {
            updateTongTien();


        }
        private List<InvoiceDetail> temporaryInvoiceDetails = new List<InvoiceDetail>();
        private void buttonThem_Click(object sender, EventArgs e)
        {
            if (!KTInput())
            {
                return;
            }

            DateTime ngayMua = dateTimeNgayMua.Value;
            string note = txtGhiChu.Text;
            string sanPhamName = cbbSanPham.Text;

            // Tìm dòng chứa sản phẩm có tên trùng với sản phẩm đã chọn
            DataGridViewRow existingRow = null;
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string productNameInGrid = row.Cells[1].Value.ToString();
                if (productNameInGrid == sanPhamName)
                {
                    existingRow = row;
                    break;
                }
            }

            if (existingRow != null)
            {
                // Nếu sản phẩm đã tồn tại, cập nhật số lượng và tổng tiền
                int soLuong = int.Parse(txtSoLuong.Text);
                decimal donGia = decimal.Parse(txtDonGia.Text);
                decimal tongTien = donGia * soLuong;

                existingRow.Cells[2].Value = soLuong;
                existingRow.Cells[4].Value = tongTien;
                Clear_Input();


            }
            else
            {
                // Nếu sản phẩm chưa tồn tại, thêm sản phẩm mới
                string productId = modelProducts.Products
                    .Where(p => p.ProductName == sanPhamName)
                    .Select(p => p.ProductId)
                    .FirstOrDefault();

                if (productId != null)
                {
                    decimal donGia = decimal.Parse(txtDonGia.Text);
                    int soLuong = int.Parse(txtSoLuong.Text);
                    decimal tongTien = donGia * soLuong;

                    int index = dataGridView1.Rows.Add();
                    dataGridView1.Rows[index].Cells[0].Value = index + 1; // STT
                    dataGridView1.Rows[index].Cells[1].Value = sanPhamName;
                    dataGridView1.Rows[index].Cells[2].Value = soLuong;
                    dataGridView1.Rows[index].Cells[3].Value = donGia;
                    dataGridView1.Rows[index].Cells[4].Value = tongTien;

                    // Thêm vào danh sách tạm thời
                    InvoiceDetail newInvoiceDetail = new InvoiceDetail
                    {
                        ProductId = productId,
                        Quantity = soLuong,
                        Price = donGia
                    };

                    temporaryInvoiceDetails.Add(newInvoiceDetail);
                    Clear_Input();

                }
                else
                {
                    MessageBox.Show("Không tìm thấy sản phẩm!");
                }
            }

        }

       
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if(e.RowIndex >= 0 && e.ColumnIndex == 4)
            {
                UpdateTotalAmount();
            }
        }

        private void buttonThanhToan_Click(object sender, EventArgs e)
        {
            using (var transaction = modelProducts.Database.BeginTransaction())
            {
                try
                {
                    Invoice newInvoice = new Invoice
                    {
                        InvoiceDate = dateTimeNgayMua.Value,
                        Note = txtGhiChu.Text
                    };
                    decimal totalAmount = 0;
                    //modelProducts.SaveChanges();

                    foreach (var invoiceDetail in temporaryInvoiceDetails)
                    {
                        invoiceDetail.InvoiceId = newInvoice.InvoiceId;
                        modelProducts.InvoiceDetails.Add(invoiceDetail);
                        decimal itemTotal = invoiceDetail.Quantity * invoiceDetail.Price;
                        totalAmount += itemTotal;
                    }
                    modelProducts.Invoices.Add(newInvoice);
                    newInvoice.TotalAmount = totalAmount;
                    modelProducts.SaveChanges();
                    transaction.Commit();
                    MessageBox.Show("Đã thanh toán và lưu vào cơ sở dữ liệu.");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    MessageBox.Show("Lỗi: " + ex.Message);
                }
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
        private DataGridViewRow selectedRow = null;

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                selectedRow = dataGridView1.Rows[e.RowIndex];
            }
            else
            {
                selectedRow = null;
            }
        }
    }
}
