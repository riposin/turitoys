string? header, option;

header = """
	Turistore toys - QA
	-------------------
	Select an option or empty to close

	A: Validate or set the s4h_id of materials to OITM table based on SKU (MySQL)
	B: Verify material prices were updated correctly on table ITM1
	C: Validate or set the s4h_id of materials to OITM table based on SKU (Microsoft SQL Server)
	""";

Console.WriteLine(header);
option = Console.ReadLine();
option = string.IsNullOrEmpty(option) ? option : option.Trim().ToLower();

switch (option)
{
	case "a":
		Program.Optiona();
		break;
	case "b":
		Program.Optionb();
		break;
	case "c":
		Program.Optionc();
		break;
	default:
		break;
}

Console.WriteLine("""

	Done, press any key to exit...
	""");
Console.ReadLine();