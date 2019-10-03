
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace SDEChallenge.Controllers
{


    public class CustomerCSVUploadController : Controller
    {
        string T24Response = "";

        string sql = "";
        Dictionary<string, string> where = new Dictionary<string, string>();

        System.Data.SqlClient.SqlDataReader rd;

        //connectionstring
        string CS = "";


        #region Employee(s) Bulk Upload
        [HttpPost]
        public ActionResult UploadEmployeeCSV(HttpPostedFileBase postedFile)
        {
            List<string> errors = new List<string>();

            string filePath = string.Empty;
            if (postedFile != null)
            {
                string path = Server.MapPath("~/Uploads/");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                filePath = path + Path.GetFileName(postedFile.FileName);
                string extension = Path.GetExtension(postedFile.FileName);
                postedFile.SaveAs(filePath);
                //Create a DataTable.
                DataTable dt = new DataTable();
                dt.Columns.AddRange(new DataColumn[4] {

                                new DataColumn("employeeId", typeof(int)),
                                new DataColumn("managerID",typeof(int)),
                                new DataColumn("ceoID", typeof(int)),
                                new DataColumn ("employeeSalary",typeof(Decimal)),


                });
                //validating the salary Input to numeric
                //var isNumeric = int.TryParse("employeeSalary", out int n);
                //if (!isNumeric)
                //{
                //    errors.Add("The Salary Value Must be Numeric.");
                //}
                //Read the contents of CSV file.
                string csvData = System.IO.File.ReadAllText(filePath);

                //Execute a loop over the rows.
                foreach (string row in csvData.Split('\n'))
                {
                    if (!string.IsNullOrEmpty(row))
                    {
                        dt.Rows.Add();
                        int i = 0;

                        //Execute a loop over the columns.
                        foreach (string cell in row.Split(','))
                        {
                            dt.Rows[dt.Rows.Count - 1][i] = cell;
                            i++;
                        }
                    }
                }

                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["DatabaseConnection"].ToString()))
                {
                    using (SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(con))
                    {


                        //Set the database table name.--fields mappings
                        sqlBulkCopy.DestinationTableName = "dbo.Employees";
                        sqlBulkCopy.ColumnMappings.Add("employeeId", "employeeId");
                        sqlBulkCopy.ColumnMappings.Add("managerID", "managerID");
                        sqlBulkCopy.ColumnMappings.Add("CeoID", "CeoID");
                        sqlBulkCopy.ColumnMappings.Add("employeeSalary", "employeeSalary");

                        con.Open();
                        sqlBulkCopy.WriteToServer(dt);
                        TempData["Message"] = "The Employee(s) has been uploaded Successfully";
                        con.Close();
                    }
                }
            }

            return RedirectToAction("BulkUploadEmployees");
        }

        #endregion
        #region EndPoint -an instance method that returns the salary budget from the specified manager.

        [Route("returnManagerSalaryBudget")]
        [HttpGet]
        public ActionResult returnManagerSalaryBudget(string managerID)
        {
            List<object> data = new List<object>();

            // First Return salary of employees under the specified manager
            //establish relationship between employees and managers---it is a one to many relationship using the managerId
            SqlConnection con = new SqlConnection();
            string salaryBudget = "select sum(a.EmployeesSalary ) + b.ManagerSalary  as salary from Employees a innerjoin Managers on a.managerid=b.managerid and a.managerid= @managerID";
            SqlCommand cmds = new SqlCommand(salaryBudget, con);
            cmds.Parameters.AddWithValue("@managerID", managerID);
            try
            {
                con.ConnectionString = CS;
                con.Open();
                using (SqlDataReader read = cmds.ExecuteReader())
                {
                    while (read.Read())
                    {
                        data.Add(new
                        {

                            salary = read["salary"].ToString(),

                        });

                    }
                }
            }
            finally
            {
                con.Close();
            }

            return Json(new { data });
        }
        #endregion



    }
}