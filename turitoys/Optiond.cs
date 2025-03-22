using Microsoft.VisualBasic.FileIO;
using System.Data.Common;
using System.Data;
using Microsoft.Data.SqlClient;
using MySqlConnector;

public static partial class Program
{
	public static void Optiond()
	{
		string? option;
		string? sucursal;

		Console.Clear();
		Console.WriteLine("""
		Selected option was D

		Description:
		It creates the INSERT statement for the OITM table (Microsoft SQL Server) taking the data from another OITM table where such material exists (MySQL).

		Prerequisites:
		1 - A CSV file named optiondin.csv which is the result of running option C (optioncout.csv.txt)
		2 - A TXT file named optiondcnn.txt with two lines, first one for the connection string to the SQLServer, and the second one for the connection string to MySQL
		3 - Both DB must contain at least the OITM table
		4 - Both OITM tables have at least the columns:
		4.1 - ItemCode, Sucursal, ItemName, FrgnName, UnidadVenta, UnidadCompra, ItmsGrpCod, GrupoProducto, VATLiable, CodeBars, SWW, SalUnitMsr, frozenFor, PrchseItem,
		4.2 - SellItem, InvntItem,  TaxCodeAR, TaxCodeAP, U_GTS_IMPUESTOLOCAL, TreeType, S4H_Id, GestionaLote, UnidadInvent, NumInvent, DenInvent, NumCompra, DenCompra
		5 - All prerequisite files are stored next to this turitoys app

		Results:
		1 - A csv file named optiondout.csv.txt containing the result of the processing
		2 - A log file named optiondlog.txt
		3 - All result files have the current date and time as preffix

		Notes:
		1 - Be aware that it is not evaluated if the material does not already exist in MSSQL before inserting, so if the table accepts duplicates, the query will execute correctly.

		Press Y to create the SQL statement and execute it.
		Press V to just create the SQL statement
		Press any other or empty to close
		""");
		option = Console.ReadLine();
		option = string.IsNullOrEmpty(option) ? option : option.Trim().ToLower();

		if (option != "y" && option != "v") { return; }

		Console.WriteLine();
		Console.WriteLine("Type the number for sucursal field, usually the one from which the data will be taken (MySQL)");
		sucursal = Console.ReadLine();
		sucursal = string.IsNullOrEmpty(sucursal) ? sucursal : sucursal.Trim().ToLower();

		if (string.IsNullOrEmpty(sucursal)) { return; }

		string sessionID = DateTime.Now.ToString("yyyyMMddHHmmss");
		string logFileFullPath = sessionID + "_optiondlog.txt";
		StreamWriter logWriter = File.CreateText(logFileFullPath);
		string message = "";

		logWriter.WriteLine("Option selected: " + option);
		logWriter.WriteLine("Sucursal entered: " + sucursal);

		message = "Reading CSV file, please wait...";
		logWriter.WriteLine(message);
		Console.WriteLine("\n" + message);

		DataTable csvData = new();

		try
		{
			using TextFieldParser csvReader = new("optiondin.csv");
			csvReader.TextFieldType = FieldType.Delimited;
			csvReader.SetDelimiters([","]);
			csvReader.HasFieldsEnclosedInQuotes = true;
			string[]? colFields;
			bool tableCreated = false;
			colFields = csvReader.ReadFields();

			if (colFields != null)
			{
				while (tableCreated == false)
				{
					foreach (string column in colFields)
					{
						DataColumn datecolumn = new(column)
						{
							AllowDBNull = true
						};
						csvData.Columns.Add(datecolumn);
					}
					tableCreated = true;
				}
				while (!csvReader.EndOfData)
				{
					string[]? itemFields;
					itemFields = csvReader.ReadFields();
					if (itemFields != null)
					{
						csvData.Rows.Add(itemFields);
					}
				}
				message = "CSV: " + csvData.Rows.Count + " materials that not found were found lol :V";
				logWriter.WriteLine(message);
				Console.WriteLine(message);
			}
			else
			{
				message = "CSV: No columns info in first row";
				logWriter.WriteLine(message);
				logWriter.Dispose();
				Console.WriteLine(message);
				return;
			}
		}
		catch (Exception e)
		{
			message = "CSV: Error during reading - " + e.Message;
			logWriter.WriteLine(message);
			logWriter.Dispose();
			Console.WriteLine(message);
			return;
		}


		message = "\nOpening connections, please wait...";
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		string? cnnStringMS = "";
		string? cnnStringMY = "";
		SqlConnection cnnMS;
		MySqlConnection cnnMY;

		try
		{
			using StreamReader read = new("optiondcnn.txt");
			
			cnnStringMS = read.ReadLine();
			message = String.IsNullOrEmpty(cnnStringMS) ? "" : cnnStringMS;

			cnnStringMY = read.ReadLine();
			message = String.IsNullOrEmpty(cnnStringMY) ? "" : cnnStringMY;

			read.Close();
		}
		catch (Exception e)
		{
			message = "TXT: Error during reading optiondcnn.txt file - " + e.Message;
			logWriter.WriteLine(message);
			logWriter.Dispose();
			Console.WriteLine(message);
			return;
		}


		cnnMS = new(cnnStringMS);
		cnnMY = new(cnnStringMY);

		message = "Connection MSSQL to use is: " + cnnStringMS;
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		message = "Connection MySQL to use is: " + cnnStringMY;
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		try
		{
			cnnMS.OpenAsync().Wait();
		}
		catch (Exception e)
		{
			message = "MSSQL: Error during connection - " + e.Message;
			logWriter.WriteLine(message);
			logWriter.Dispose();
			Console.WriteLine(message);
			return;
		}

		try
		{
			cnnMY.OpenAsync().Wait();
		}
		catch (Exception e)
		{
			cnnMS.Close();

			message = "MySQL: Error during connection - " + e.Message;
			logWriter.WriteLine(message);
			logWriter.Dispose();
			Console.WriteLine(message);

			return;
		}


		message = "\nGetting all materials from DB to process locally, please wait...";
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		MySqlCommand command;
		MySqlDataReader readerAll;
		List<string[]> resultAll = [];
		IReadOnlyCollection<DbColumn> columns;
		string sqlGetMaterialTemplateMy;
		string sqlGetMaterialMy;
		// ItemCode always must be first element, check linq.first statement when verifying
		sqlGetMaterialTemplateMy = "SELECT " +
									"ItemCode, '{0}' as Sucursal, ItemName, FrgnName, if(UnidadVenta is null, 'PC', UnidadVenta) as UnidadVenta, UnidadCompra, ItmsGrpCod, " +
									"if(GrupoProducto is null, '', GrupoProducto) as GrupoProducto,VATLiable, CodeBars, SWW, SalUnitMsr, frozenFor, PrchseItem, SellItem, " +
									"InvntItem,  TaxCodeAR, TaxCodeAP, U_GTS_IMPUESTOLOCAL, TreeType, S4H_Id, GestionaLote, UnidadInvent, NumInvent, DenInvent, NumCompra, DenCompra " +
									"FROM oitm;";
		sqlGetMaterialMy = string.Format(sqlGetMaterialTemplateMy, sucursal, sucursal);
		message = sqlGetMaterialMy;
		logWriter.WriteLine(message);
		command = new MySqlCommand(message, cnnMY);
		readerAll = command.ExecuteReader();
		resultAll = [];
		columns = readerAll.GetColumnSchema();

		while (readerAll.Read())
		{
			string[] item = [];
			foreach (DbColumn col in columns)
			{
				item = [.. item, (string)(readerAll[col.ColumnName] == DBNull.Value ? "" : readerAll[col.ColumnName].ToString())];
			}
			resultAll.Add(item);
		}
		readerAll.Close();

		if (resultAll.Count == 0)
		{
			message = "MySQL: No materials found";
			logWriter.WriteLine(message);
			logWriter.Dispose();
			cnnMS.Close();
			cnnMY.Close();
			Console.WriteLine(message);
			return;
		}
		else
		{
			message = "MySQL: " + resultAll.Count + " materials found";
			logWriter.WriteLine(message);
			Console.WriteLine(message);
		}


		message = "\nProcessing materials in CSV against DBs, please wait...";
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		string outFileFullPath = sessionID + "_optiondout.csv.txt";
		StreamWriter outWriter = File.CreateText(outFileFullPath);
		int notFoundItems = 0;
		int insertedItems = 0;
		string? csvMaterialStatusDB = "";
		string? csvMaterialSKU = "";
		string? csvMaterialS4H = "";
		string? csvMaterialDES = "";
		IEnumerable<string[]> query;
		bool insertMaterial = false;
		int csvDataRowsCount = csvData.Rows.Count;
		List<string[]> dbMaterial;
		int insertResult = -1;
		string columnsToSet = "";
		string sqlInsertMaterialTemplate;
		string sqlInsertMaterial = "";
		bool first = true;
		string materialData = "";
		SqlCommand commandMS;

		sqlInsertMaterialTemplate = "INSERT INTO oitm ({0}) VALUES({1});";

		foreach (DbColumn col in columns)
		{
			if (first)
			{
				columnsToSet = col.ColumnName;
			}
			else
			{
				columnsToSet += "," + col.ColumnName;
			}
			first = false;
		}

		outWriter.WriteLine("sku,mat,des,statusdb,result");
		insertMaterial = (option == "y");

		foreach (DataRow mat in csvData.Rows)
		{
			csvMaterialStatusDB = String.IsNullOrEmpty(mat["statusdb"].ToString()) ? "" : mat["statusdb"].ToString();
			csvMaterialSKU = String.IsNullOrEmpty(mat["sku"].ToString()) ? "" : mat["sku"].ToString();
			csvMaterialS4H = String.IsNullOrEmpty(mat["mat"].ToString()) ? "" : mat["mat"].ToString();
			csvMaterialDES = String.IsNullOrEmpty(mat["des"].ToString()) ? "" : mat["des"].ToString();

			if (csvMaterialStatusDB == "not found")
			{
				// Try to get the material from DB
				query = resultAll.Where(arr => arr.First() == csvMaterialSKU);
				if (query.Count() == 0)
				{
					notFoundItems++;

					message = csvMaterialSKU + " not found";
					logWriter.WriteLine(message);
					outWriter.WriteLine("\"" + csvMaterialSKU + "\"," + csvMaterialS4H + ",\"" + csvMaterialDES + "\",not found my," + "ignored");
				}
				else
				{
					logWriter.WriteLine("The material SKU " + csvMaterialSKU + " was found");
					dbMaterial = query.ToList();

					for (int i = 0; i < columns.Count; i++)
					{
						if (i == 0)
						{
							materialData = "'" + dbMaterial[0][i] + "'";
						}
						else
						{
							materialData += ",'" + dbMaterial[0][i] + "'";
						}
					}
					sqlInsertMaterial = string.Format(sqlInsertMaterialTemplate, columnsToSet, materialData);

					message = "\"" + csvMaterialSKU + "\"," + csvMaterialS4H + ",\"" + csvMaterialDES + "\"" + (insertMaterial ? ",{0}," + "\"" + sqlInsertMaterial + "\"" : ",found my,\"" + sqlInsertMaterial + "\"");

					if (insertMaterial)
					{
						commandMS = new SqlCommand(sqlInsertMaterial, cnnMS);
						try
						{
							insertResult = commandMS.ExecuteNonQuery();

							if(insertResult < 1)
							{
								logWriter.WriteLine("Material with issue in INSERT: " + sqlInsertMaterial);
								message = string.Format(message, "error insert");
							}
							else
							{
								insertedItems += 1;
								message = string.Format(message, "inserted");
							}
						}
						catch (Exception ex)
						{
							logWriter.WriteLine("Material with issue in INSERT: " + sqlInsertMaterial);
							logWriter.WriteLine(ex.ToString());
							message = string.Format(message, "error insert");
						}
					}

					outWriter.WriteLine(message);
				}
			}

		}
		cnnMS.Close();
		cnnMY.Close();
		outWriter.Dispose();
		resultAll.TrimExcess();
		csvData.Dispose();

		message = notFoundItems > 0 ? notFoundItems + " materials with statusdb 'not found' were not found in MyDB" : "All materials with statusdb 'not found' were found in MyDB";
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		message = insertedItems + " materials inserted in MSDB";
		logWriter.WriteLine(message);
		Console.WriteLine(message);

		logWriter.Dispose();

		return;
	}
}