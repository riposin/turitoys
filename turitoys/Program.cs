using Microsoft.VisualBasic.FileIO;
using MySqlConnector;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

string? header, option;

header = """
    Turistore toys - QA
    -------------------
    Select an option or empty to close

    A: Set materials s4hid from csv to db pos based on sku
    """;

Console.WriteLine(header);
option = Console.ReadLine();
option = string.IsNullOrEmpty(option) ? option : option.Trim().ToLower();

switch (option)
{
	case "a":
		optiona();
		break;
	default:
		break;
}

Console.WriteLine("""

    Done, press any key to exit...
    """);
Console.ReadLine();

static void optiona()
{
	string? option;
	DataTable csvData = new();
	MySqlConnection cnn = new("Server=lapqa.hamachi;User ID=root;Password=Brutus22;database=gts");
	string? materialSKU = "";
	string sqlGetMaterial = "select itemcode as sku, itemname as des, s4h_id as mat from oitm where oitm.ItemCode = '{0}';";
	MySqlCommand command;

	Console.Clear();
	Console.WriteLine("""
    Selected option was A
    
    Prerequisites:
    1 - A csv file named optionain.csv with 3 columns [mat,des,sku] at least
    2 - A text file named optionacnn.txt with the connection string to the MySQL database of an All Retail POS installation
    3 - All files are stored next to this turitoys app

    Results:
    1 -

    Press Y to proceed or empty to close
    """);
	option = Console.ReadLine();
	option = string.IsNullOrEmpty(option) ? option : option.Trim().ToLower();

	if (option != "y") { return; }

	try
	{
		using TextFieldParser csvReader = new("C:\\Users\\JesúsRicardoPoolPech\\Documents\\QA\\z_PrerequisitosInsumos\\PreciosMasivo\\optionain.csv");
		csvReader.TextFieldType = FieldType.Delimited;
		csvReader.SetDelimiters(new string[] { "," });
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
		}
		else
		{
			Console.WriteLine("CSV: No columns info in first row");
		}
	}
	catch (Exception e)
	{
		Console.WriteLine("CSV: Error during reading - " + e.Message);
	}

	try
	{
		cnn.OpenAsync().Wait();
	}
	catch (Exception e)
	{
		Console.WriteLine("MySQL: Error during connection - " + e.Message);
		return;
	}

	int notFoundItems = 0;

	// SKU always must be first element, check linq.first statement when verifying
	command = new MySqlCommand("select itemcode as sku, itemname as des, s4h_id as mat from oitm;", cnn);

	var readerAll = command.ExecuteReader();
	List<string[]> resultAll = [];
	IReadOnlyCollection<DbColumn> columns = readerAll.GetColumnSchema();
	Console.WriteLine("Getting all materials from db to process locally, please wait...");

	while (readerAll.Read())
	{
		string[] item = [];
		foreach (DbColumn col in columns)
		{
			item = [.. item, (string)(readerAll[col.ColumnName] == DBNull.Value ? "" : readerAll[col.ColumnName])];
		}
		resultAll.Add(item);
	}
	readerAll.Close();

	if (resultAll.Count == 0)
	{
		Console.WriteLine("No materials found");
	}
	else
	{
		Console.WriteLine("Found " + resultAll.Count + " materials");
	}

	Console.WriteLine("Verifying if materials in CSV exists in materials retrieved from db, please wait...");
	foreach (DataRow mat in csvData.Rows)
	{
		materialSKU = String.IsNullOrEmpty(mat["sku"].ToString()) ? "" : mat["sku"].ToString();
		var query = resultAll.Where(arr => arr.First() == materialSKU);
		if(query.Count() == 0)
		{
			notFoundItems++;
			// echo the items not found
		}
		else
		{
			// get the materials from query and compare sku
			// if didn't match, set the s4hid
		}
	}
	Console.WriteLine(notFoundItems > 0 ? notFoundItems + " materials were not found in db of " + csvData.Rows.Count + " in csv" : "All materials were found");



	// One quey per material directly to db, not efficient!
	foreach (DataRow mat in csvData.Rows)
	{
		/*
		aux ++;
		materialSKU = String.IsNullOrEmpty(mat["sku"].ToString()) ? "" : mat["sku"].ToString();
		command = new MySqlCommand(string.Format(sqlGetMaterial, materialSKU), cnn);

		var reader = command.ExecuteReader();
		List<string[]> result = [];

		while (reader.Read())
		{
			string[] item = [];
			foreach (DbColumn col in reader.GetColumnSchema())
			{
				item = [.. item, (string)reader[col.ColumnName]];
			}
			result.Add(item);
		}
		reader.Close();

		if (result.Count == 0)
		{
			notFoundItems ++;
			Console.WriteLine("Not found " + materialSKU);
		}
		else
		{
			Console.WriteLine("Item code for " + materialSKU + ": " + result[0][2]);
		}
		*/

		//Console.WriteLine(result.Count == 0 ? "Not found " + materialSKU : "Item code for " + materialSKU + ": " + result[0][2]);

		/*if (aux > 10)
		{
			break;
		}*/
	}

	Console.WriteLine("Not found items count: " + notFoundItems);

	cnn.Close();

	return;
}