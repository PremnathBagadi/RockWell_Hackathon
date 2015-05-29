using System;
using System.Text;

namespace ROCKWELL_WIN
{
	/// <summary>
	/// SQL Server only ignores adjacent duplicated phonetic sounds
	/// Plus, it doesn't ignore a character if it is duplicated with the leading char
	/// 
	/// For example, SQL Server will encode "PPPP" as "P100", whereas Miracode will
	/// encode it as "P000".
	/// </summary>
	internal class SQLServerSoundEx : ISoundEx {

		public override string GenerateSoundEx(string s) {
			StringBuilder output=new StringBuilder();

			if(s.Length>0) {

				output.Append(Char.ToUpper(s[0]));

				// Stop at a maximum of 4 characters
				for(int i=1; i<s.Length && output.Length<4; i++) {
					string c=EncodeChar(s[i]);

					// Ignore duplicated chars, except a duplication with the first char
					if(i==1) {
						output.Append(c);
					} else if(c!=EncodeChar(s[i-1])) { 
						output.Append(c);
					}
				} 

				// Pad with zeros
				for(int i=output.Length; i<4; i++) {
					output.Append("0");
				}
			}
	
			return output.ToString();
		}

		private void AssertEquals(string s1, string s2, string error) {
			if(!s1.Equals(s2))
				throw new Exception(error + ". Expected " + s2 + " but got " + s1);
		}
		public override void ValidateAlgorithm() {
			// Validate the SoundEx agorithm

			AssertEquals(GenerateSoundEx("Tymczak"),"T522", "SoundEx Algoritm Broken");
			AssertEquals(GenerateSoundEx("Ashcraft"),"A226", "SoundEx Algoritm Broken");
			AssertEquals(GenerateSoundEx("Pfister"),"P123", "SoundEx Algoritm Broken");
			AssertEquals(GenerateSoundEx("Jackson"),"J250", "SoundEx Algoritm Broken");
			AssertEquals(GenerateSoundEx("Gutierrez"),"G362", "SoundEx Algoritm Broken");
			AssertEquals(GenerateSoundEx("VanDeusen"),"V532", "SoundEx Algoritm Broken");
			AssertEquals(GenerateSoundEx("Deusen"),"D250", "SoundEx Algoritm Broken");
		}


	}
}
