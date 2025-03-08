// See https://aka.ms/new-console-template for more information
using Microsoft.VisualBasic.FileIO;
using MySqlConnector;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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


	DataTable csvData = new();
	//string jsonString;
	MySqlConnection cnn = new("Server=lapqa.hamachi;User ID=root;Password=Brutus22;database=gts");

	try
	{
		using TextFieldParser csvReader = new("C:\\Users\\JesúsRicardoPoolPech\\Documents\\QA\\z_PrerequisitosInsumos\\PreciosMasivo\\optionain.csv");
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
					csvData.Rows.Add();
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
		Console.WriteLine("Error during connection: " + e.Message);
		return;
	}

	var command = new MySqlCommand("select itemcode as sku, itemname as des, s4h_id as mat from oitm where oitm.ItemCode = '4005800144592';", cnn);

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
	Console.WriteLine(result.Count == 0 ? "Not found" : result[0][2]);

	cnn.Close();

	return;
}