﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Web.Models
{
    public static class DatabaseConnector
    {
        private static string connectionstring = "Server=ivmsdb.cs17etkshc9t.us-east-1.rds.amazonaws.com,1433;Database=ivmsdb;User ID=admin;Password=ivmsdbadmin;Trusted_Connection=false;";

        /// <summary>
        /// Convert Datatable to Dictionary
        /// </summary>
        /// <param name="dataTable">Datatable</param>
        /// <returns>Dictionary</returns>
        public static Dictionary<int, Dictionary<string, string>> DataTableToDictionary(DataTable dataTable)
        {
            Dictionary<int, Dictionary<string, string>> result = new Dictionary<int, Dictionary<string, string>>();
            if (dataTable != null)
            {
                int rowNum = 0;
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var rowDetail = new Dictionary<string, string>();
                    foreach (DataColumn dataColumn in dataTable.Columns)
                    {
                        rowDetail.Add(dataColumn.ColumnName, dataRow[dataColumn].ToString());
                    }
                    result.Add(rowNum, rowDetail);
                    rowNum++;
                }
            }
            else
            {
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Convert Datatable to Dictionary
        /// </summary>
        /// <param name="dataTable">Datatable</param>
        /// <returns>Dictionary</returns>
        public static ConcurrentBag<Dictionary<string, string>> DataTableToConcurrentBag(DataTable dataTable)
        {
            ConcurrentBag<Dictionary<string, string>> result = new ConcurrentBag<Dictionary<string, string>>();
            if (dataTable != null)
            {
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    var rowDetail = new Dictionary<string, string>();
                    foreach (DataColumn dataColumn in dataTable.Columns)
                    {
                        rowDetail.Add(dataColumn.ColumnName, dataRow[dataColumn].ToString());
                    }
                    result.Add(rowDetail);
                }
            }
            else
            {
                result = null;
            }
            return result;
        }

        /// <summary>
        /// Private method for checking user info by ID directory.
        /// </summary>
        /// <param name="ID">User ID</param>
        /// <returns>A dictionary contains user info.</returns>
        private static Dictionary<string, string> checkStatusByID(int ID)
        {
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select UserName, UserEmail, UserStatus from AccountLogin join HealthStatus on AccountLogin.ID = HealthStatus.ID where AccountLogin.ID = {ID}", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            // Convert table to dictionary
            var result = DataTableToDictionary(ds.Tables[0]);

            return result.Count > 0 ? result[0] : new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Visitor account not exist."}
                };
        }

        /// <summary>
        /// Private method for checking user id by email.
        /// </summary>
        /// <param name="userEmail">User email</param>
        /// <returns>Return user id.</returns>
        private static int getUserID(string userEmail)
        {
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select ID from AccountLogin where UserEmail = '{userEmail}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return 0;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            var result = DataTableToDictionary(ds.Tables[0]);
            return result.Count > 0 ? int.Parse(result[0]["ID"]) : 0;
        }

        /// <summary>
        /// Private method for getting a list of guard devices by guard id.
        /// </summary>
        /// <param name="ID">Guard id</param>
        /// <returns>A list of guard devices.</returns>
        private static Dictionary<int, Dictionary<string, string>> getGuardDevices(int ID)
        {
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select DeviceID, Description from GuardDevices where ID = {ID}", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    var temp = new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };

                    return new Dictionary<int, Dictionary<string, string>>
                    {
                        {0, temp}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            return DataTableToDictionary(ds.Tables[0]);
        }

        private static int deviceOwner(string deviceID)
        {
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select ID from GuardDevices where DeviceID = '{deviceID}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return 0;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            var result = DataTableToDictionary(ds.Tables[0]);


            return result.Count > 0 ? int.Parse(result[0]["ID"]) : 0;
        }

        private static Boolean userActivityUpdate(int userID, int guardID)
        {
            if (guardID <= 0)
            {
                return false;
            }

            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select Visitor_ID from CurrentContact where Guard_ID = {guardID}", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return false;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            var visitorList = DataTableToConcurrentBag(ds.Tables[0]);
            try
            {
                Parallel.ForEach(visitorList, (v) => {
                    using (SqlConnection connection = new SqlConnection(connectionstring))
                    {
                        try
                        {
                            connection.Open();
                            SqlDataAdapter adp = new SqlDataAdapter($"insert into PersonalContact(ID, Contact_ID, Guard_ID, StartTime) values ({userID}, {v["Visitor_ID"]}, {guardID}, GETDATE())", connection);
                            adp.Fill(ds);
                        }
                        catch (Exception e)
                        {
                            throw;
                        }
                        finally
                        {
                            if (connection.State == ConnectionState.Open)
                                connection.Close();
                        }
                    }
                });
            }
            catch (Exception e)
            {
                return false;
            }

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"insert into CurrentContact (Visitor_ID, Guard_ID) values ({userID}, {guardID})", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return false;
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            return true;
        }

        /// <summary>
        /// Registor an account by using a new email.
        /// </summary>
        /// <param name="userName">User name</param>
        /// <param name="userEmail">User email</param>
        /// <param name="userPassword">User password</param>
        /// <param name="userRole">User role</param>
        /// <returns>A dictionary that can indicate whether the procress is success or not.</returns>
        public static Dictionary<string,string> userRegister(string userName, string userEmail, string userPassword, int userRole)
        {
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select UserEmail from AccountLogin where UserEmail = '{userEmail}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            var result = DataTableToDictionary(ds.Tables[0]);
            if (userEmail == null)
            {
                return new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Unknow email."}
                };
            }
            else if (result.Count > 0)
            {
                return new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Account already exist."}
                };
            }

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"insert into AccountLogin (UserName, UserEmail, UserPassword, UserRole) values ('{userName}', '{userEmail}', '{userPassword}', '{userRole}')", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            if (userRole == 1) 
            {
                var id = getUserID(userEmail);

                using (SqlConnection connection = new SqlConnection(connectionstring))
                {
                    try
                    {
                        connection.Open();
                        SqlDataAdapter adp = new SqlDataAdapter($"insert into HealthStatus (ID, UserStatus) values ({id}, 0)", connection);
                        adp.Fill(ds);
                    }
                    catch (Exception e)
                    {
                        return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                    }
                    finally
                    {
                        if (connection.State == ConnectionState.Open)
                            connection.Close();
                    }
                }
            }

            return new Dictionary<string, string>
            {
                {"result","success"}, {"message", "Success."}
            };
        }

        /// <summary>
        /// Check user login.
        /// </summary>
        /// <param name="userEmail">User email</param>
        /// <param name="userPassword">User password</param>
        /// <param name="userRole">User role</param>
        /// <returns>If success, return a dictionary contains user name, return error message otherwise.</returns>
        public static Dictionary<string, string> userLogin(string userEmail, string userPassword, int userRole)
        {
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select UserName, UserEmail, UserPassword, UserRole from AccountLogin where UserEmail = '{userEmail}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }
            
            // Convert table to dictionary
            var result = DataTableToDictionary(ds.Tables[0]);

            if (userEmail == null || result.Count == 0)
            {
                return new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Account not exist."}
                };
            }
            else if (userPassword == null || !userPassword.Equals(result[0]["UserPassword"]))
            {
                return new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Password incorrect."}
                };
            }
            else if (userRole != int.Parse(result[0]["UserRole"]))
            {
                return new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Usre didn't have permission in this role."}
                };
            }
            else
            {
                return new Dictionary<string, string>
                {
                    {"result","success"}, {"message", $"{result[0]["UserName"]}"}
                };
            }
        }

        /// <summary>
        /// Check visitor's health status for doctor.
        /// </summary>
        /// <param name="userEmail">Current user email</param>
        /// <param name="userPassword">Current user password</param>
        /// <param name="visitorEmail">Visitor Email</param>
        /// <returns>A dictionary contains health status.</returns>
        public static Dictionary<string, string> checkPatientStatus(string userEmail, string userPassword, string visitorEmail)
        {
            var check = userLogin(userEmail, userPassword, 3);
            if (!check["result"].Equals("success"))
            {
                return check;
            }

            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select UserName, UserEmail, UserStatus from AccountLogin join HealthStatus on AccountLogin.ID = HealthStatus.ID where UserEmail = '{visitorEmail}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            // Convert table to dictionary
            var result = DataTableToDictionary(ds.Tables[0]);

            return result.Count > 0 ? result[0] : new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Visitor account not exist."}
                };
        }

        /// <summary>
        /// Update visitor's health status.
        /// </summary>
        /// <param name="userEmail">Current user email</param>
        /// <param name="userPassword">Current user password</param>
        /// <param name="visitorEmail">Visitor email</param>
        /// <param name="status">Health status</param>
        /// <returns>A dictionary contains updated health status.<</returns>
        public static Dictionary<string, string> updatePatientStatus(string userEmail, string userPassword, string visitorEmail, float status)
        {
            // Check permission
            var check = userLogin(userEmail, userPassword, 3);
            if (!check["result"].Equals("success"))
            {
                return check;
            }

            // Get User ID
            var VisitorID = getUserID(visitorEmail);
            if (VisitorID == 0)
            {
                return new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Account not exist."}
                };
            }

            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"update HealthStatus set UserStatus = {status} WHERE ID = {VisitorID}", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            return checkStatusByID(VisitorID);
        }

        /// <summary>
        /// Self-checking user's health status.
        /// </summary>
        /// <param name="userEmail">Current user email</param>
        /// <param name="userPassword">Current user password</param>
        /// <returns>A dictionary contains health status.</returns>
        public static Dictionary<string, string> checkUserStatus(string userEmail, string userPassword)
        {
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select UserName, UserStatus from AccountLogin join HealthStatus on AccountLogin.ID = HealthStatus.ID where UserEmail = '{userEmail}' and UserPassword = '{userPassword}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            // Convert table to dictionary
            var result = DataTableToDictionary(ds.Tables[0]);

            return result.Count > 0 ? result[0] : new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "User health status not exist."}
                };
        }

        /// <summary>
        /// Get guard devices by guard email and password.
        /// </summary>
        /// <param name="userEmail">Guard email</param>
        /// <param name="userPassword">Guard password</param>
        /// <returns>A dictionary of guard devices.</returns>
        public static Dictionary<int, Dictionary<string, string>> getGuardDevices(string userEmail, string userPassword)
        {
            // Check permission
            var check = userLogin(userEmail, userPassword, 2);
            if (!check["result"].Equals("success"))
            {
                return new Dictionary<int, Dictionary<string, string>>
                {
                    {0, check}
                };
            }

            // Get User ID
            var id = getUserID(userEmail);
            if (id == 0)
            {
                var temp = new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Account not exist."}
                };

                return new Dictionary<int, Dictionary<string, string>>
                {
                    {0, temp}
                };
            }

            return getGuardDevices(id);
        }

        /// <summary>
        /// Register new guard devices.
        /// </summary>
        /// <param name="userEmail">Guard email</param>
        /// <param name="userPassword">Guard password</param>
        /// <param name="deviceID">Device id</param>
        /// <param name="deviceDescription">Device description</param>
        /// <returns>A dictionary of updated guard devices.</returns>
        public static Dictionary<int, Dictionary<string, string>> registerGuardDevice(string userEmail, string userPassword, string deviceID, string deviceDescription)
        {
            // Check permission
            var check = userLogin(userEmail, userPassword, 2);
            if (!check["result"].Equals("success"))
            {
                return new Dictionary<int, Dictionary<string, string>>
                {
                    {0, check}
                };
            }

            // Get User ID
            var id = getUserID(userEmail);
            if (id == 0)
            {
                var temp = new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Account not exist."}
                };

                return new Dictionary<int, Dictionary<string, string>>
                {
                    {0, temp}
                };
            }

            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"insert into GuardDevices (ID, DeviceID, Description, VisitorEmail, VisitorTemperature, LastUpdated) values ({id}, '{deviceID}', '{deviceDescription}', 'U1@test', 37, GETDATE()); ", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    var temp = new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };

                    return new Dictionary<int, Dictionary<string, string>>
                    {
                        {0, temp}
                    }; 
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            return getGuardDevices(id);
        }

        /// <summary>
        /// Delete existed guard device.
        /// </summary>
        /// <param name="userEmail">Guard email</param>
        /// <param name="userPassword">Guard password</param>
        /// <param name="deviceID">Device id</param>
        /// <returns>A dictionary of updated guard devices.</returns>
        public static Dictionary<int, Dictionary<string, string>> deleteGuardDevice(string userEmail, string userPassword, string deviceID)
        {
            // Check permission
            var check = userLogin(userEmail, userPassword, 2);
            if (!check["result"].Equals("success"))
            {
                return new Dictionary<int, Dictionary<string, string>>
                {
                    {0, check}
                };
            }

            // Get User ID
            var id = getUserID(userEmail);
            if (id == 0)
            {
                var temp = new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Account not exist."}
                };

                return new Dictionary<int, Dictionary<string, string>>
                {
                    {0, temp}
                };
            }

            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"delete from GuardDevices where ID = {id} and DeviceID = '{deviceID}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    var temp = new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };

                    return new Dictionary<int, Dictionary<string, string>>
                    {
                        {0, temp}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            return getGuardDevices(id);
        }

        /// <summary>
        /// Update visitor status to guard device list.
        /// </summary>
        /// <param name="deviceID">Device id</param>
        /// <param name="visitorEmail">Visitor Email</param>
        /// <returns>Return success in default, return exception otherwise.</returns>
        public static Dictionary<string, string> visitorDetect(string deviceID, string visitorEmail)
        {
            // Get User ID
            var id = getUserID(visitorEmail);
            if (id == 0)
            {
                return new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Account not exist."}
                };
            }

            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"update GuardDevices set VisitorID = {id}, VisitorTemperature = 0, LastUpdated = GETDATE() where DeviceID = '{deviceID}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            return new Dictionary<string, string>
            {
                {"result","success"}, {"message", "Success."}
            };
        }

        /// <summary>
        /// Check whether there is a visitor waiting for temperature scanning.
        /// </summary>
        /// <param name="deviceID">Device id</param>
        /// <returns>Return true if a visitor is waiting.</returns>
        public static Dictionary<string, string> incomingVisitorDetect(string deviceID)
        {
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select VisitorTemperature from GuardDevices where DeviceID = '{deviceID}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            var result = DataTableToDictionary(ds.Tables[0]);
            if (result.Count == 0)
            {
                return new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Device not registered."}
                };
            }

            if (Math.Abs(float.Parse(result[0]["VisitorTemperature"])) <= 0.1)
            {
                return new Dictionary<string, string>
                {
                    {"result","true"}
                };
            }
            else
            {
                return new Dictionary<string, string>
                {
                    {"result","false"}
                };
            }
            
        }

        /// <summary>
        /// Update scanned body temperature to device.
        /// </summary>
        /// <param name="deviceID">Device id</param>
        /// <param name="temperature">Visitor's body temperature</param>
        /// <returns>Return success in default, return exception otherwise.</returns>
        public static Dictionary<string, string> visitorTemperatureUpdate(string deviceID, float temperature)
        {
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"update GuardDevices set VisitorTemperature = {temperature} where DeviceID = '{deviceID}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            return new Dictionary<string, string>
            {
                {"result","success"}, {"message", "Success."}
            };
        }

        /// <summary>
        /// Check the visitor who being detected by guard device.
        /// </summary>
        /// <param name="deviceID">Device id</param>
        /// <returns>A dictionary contains visitor health status.</returns>
        public static Dictionary<string, string> visitorInfoCheck(string deviceID)
        {
            DataSet ds = new DataSet();

            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                try
                {
                    connection.Open();
                    SqlDataAdapter adp = new SqlDataAdapter($"select AccountLogin.UserName, HealthStatus.UserStatus, GuardDevices.VisitorTemperature, DATEDIFF(ss, GuardDevices.LastUpdated, GETDATE()) AS LastUpdated from GuardDevices join AccountLogin on GuardDevices.VisitorID = AccountLogin.ID join HealthStatus on GuardDevices.VisitorID = HealthStatus.ID where GuardDevices.DeviceID = '{deviceID}'", connection);
                    adp.Fill(ds);
                }
                catch (Exception e)
                {
                    return new Dictionary<string, string>
                    {
                        {"result","error"}, {"message", e.ToString()}
                    };
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                        connection.Close();
                }
            }

            // Convert table to dictionary
            var result = DataTableToDictionary(ds.Tables[0]);

            return result.Count > 0 ? result[0] : new Dictionary<string, string>
                {
                    {"result","error"}, {"message", "Device not registered."}
                };
        }
    }
}
