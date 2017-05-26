#pragma once
#include "wadhandler.h"
#include <stdio.h>
extern "C"
{
	//New title ID must be 4 chars long
	//working path must be a folder "without ending \"
	//targetPath must be a filename (including .wad)
	__declspec(dllexport) int packWad(char* pathToOpeningBnr, char* pathToTicket, char* pathToTMD, char* pathToCertFile, char* pathToCommonKey, char* newTitleID, char* workingPath, char* targetPath)
	{
		FILE *ftmd;
		FILE *ftik;
		FILE *fapp;
		FILE *fcert;
		FILE *ftrailer;
		u8 title_key[16];
		u8 *tmd;
		u8 *cert;
		u8 *trailer;
		u8 *tik;
		u8 *apps = NULL; //Encrypted files, with 64 bytes boundary
		u32 len_tmd;
		u32 len_tmd_nb;
		u32 len_tik;
		u32 len_tik_nb;
		u16 num_app;
		u32 len_cert;
		u32 len_cert_nb;
		u32 len_trailer;
		u32 len_trailer_nb;
		u64 len_apps = 0;
		char name[17];
		u8 hash[20];
		u16 i;
		u64 temp, temp2;
		u8 iv[16];
		u8 sign_tik = 0;
		u8 sign_tmd = 0;
		u8 sign_type = 1; // Sign type. if 1 watermark enabled. if 0 watermark disabled
		char *new_id = NULL;

		if (strlen(newTitleID)==4) 
		{
			new_id = (char *)malloc(4);
			memset(new_id, 0, 4);
			strncpy(new_id, newTitleID, 4);
		}
		else
		{
			return -10;
		}

		//CERT file
		fcert = fopen(pathToCertFile, "rb"); 
		if (!fcert) 
		{
			//printf("Could not find cert_file (%s)\n", argv[3]); 
			return -2;
		}
		temp = getfilesize(fcert);
		len_cert_nb = temp;
		len_cert = round_up(temp, 0x40);
		cert = (u8*)malloc(len_cert);
		memset(cert, 0, len_cert);
		fread(cert, temp, 1, fcert);
		fclose(fcert);
		//OPENING BNR
		ftrailer = fopen(pathToOpeningBnr, "rb"); 
		if (!ftrailer) 
		{
			//printf("Could not find trailer_file (00000000.app)\n"); 
			return -3;
		}
		temp = getfilesize(ftrailer);
		len_trailer_nb = temp;
		len_trailer = round_up(temp, 0x40);
		trailer = (u8*)malloc(len_trailer);
		memset(trailer, 0, len_trailer);
		fread(trailer, temp, 1, ftrailer);
		fclose(ftrailer);
		//TMD
		ftmd = fopen(pathToTMD, "rb"); 
		if (!ftmd) 
		{
			//printf("Could not find tmd_file (%s)\n", argv[2]); 
			return -4;
		}
		temp = getfilesize(ftmd);
		len_tmd_nb = temp;
		len_tmd = round_up(temp, 0x40);
		tmd = (u8*)malloc(len_tmd);
		memset(tmd, 0, len_tmd);
		fread(tmd, temp, 1, ftmd);
		num_app = be16(tmd + 0x01de);
		fclose(ftmd);

		// Ticket
		ftik = fopen(pathToTicket, "rb"); 
		if (!ftik) 
		{
			//printf("Could not find ticket_file (%s)\n", argv[1]); 
			return -5;
		}
		temp = getfilesize(ftik);
		len_tik = round_up(temp, 0x40);
		len_tik_nb = temp;
		tik = (u8*)malloc(len_tik);
		memset(tik, 0, len_tik);
		fread(tik, temp, 1, ftik);
		fclose(ftik);
		// Change title id if required
		if (new_id!=NULL) {
			memcpy(tmd + 0x0190, new_id, 4);
			memcpy(tik + 0x01E0, new_id, 4);
		}
		//Sign the ticket
		if(Ticket_resign(tik, len_tik_nb, sign_type) != 1)
			return -6;
		
		// Get Title key
		if(decrypt_title_key(tik, title_key, pathToCommonKey)!=0)
		{
			return -7;
		}

		//For banner file (since it has a separate name from *.app
		i=0;
		fapp = fopen(pathToOpeningBnr, "rb");
		if (!fapp) {
			/*printf("\nERROR: Could not find %s file.\n", name);
			printf("File TMD Description:\n");
			printf("File size: %d bytes\n", be64(tmd + 0x01ec + 0x24*i));
			printf("File SHA Hash: 0x");
			printHashSHA(tmd + 0x01F4 + (0x24*i));
			printf("\n");
			exit(-1);*/
			return -11;
		}
		temp = getfilesize(fapp);
		temp2 = round_up(temp, 0x40);
		len_apps += temp2;
		apps = (u8 *)realloc(apps, len_apps);
		memset(apps+len_apps-temp2, 0, temp2);
		fread(apps+len_apps-temp2, temp, 1, fapp);
		fclose(fapp);
		// SHA hash update
		sha(apps+len_apps-temp2, temp, hash);
		memcpy(tmd + 0x1F4 + (0x24*i), hash, 20);
		// File size update
		wbe64(tmd + 0x1EC + (0x24*i), temp);
		// Encrypt file
		memset(iv, 0, sizeof iv);
		memcpy(iv, tmd + 0x01e8 + 0x24*i, 2);
		aes_cbc_enc(title_key, iv, apps+len_apps-temp2, round_up(temp, 0x10), apps+len_apps-temp2);

		
		// Read app files
		for (i=1;i<num_app;i++) 
		{
			sprintf(name, "\\%08x.app", i);
			char* fullPath=(char*)calloc(strlen(workingPath)+strlen(name),sizeof(char));
			strcpy(fullPath, workingPath);
			strcat(fullPath, name);
			fapp = fopen(fullPath, "rb");
			if (!fapp) {
				/*printf("\nERROR: Could not find %s file.\n", name);
				printf("File TMD Description:\n");
				printf("File size: %d bytes\n", be64(tmd + 0x01ec + 0x24*i));
				printf("File SHA Hash: 0x");
				printHashSHA(tmd + 0x01F4 + (0x24*i));
				printf("\n");
				exit(-1);*/
				return -8;
			}
			temp = getfilesize(fapp);
			temp2 = round_up(temp, 0x40);
			len_apps += temp2;
			apps = (u8 *)realloc(apps, len_apps);
			memset(apps+len_apps-temp2, 0, temp2);
			fread(apps+len_apps-temp2, temp, 1, fapp);
			fclose(fapp);
			// SHA hash update
			sha(apps+len_apps-temp2, temp, hash);
			memcpy(tmd + 0x1F4 + (0x24*i), hash, 20);
			// File size update
			wbe64(tmd + 0x1EC + (0x24*i), temp);
			// Encrypt file
			memset(iv, 0, sizeof iv);
			memcpy(iv, tmd + 0x01e8 + 0x24*i, 2);
			aes_cbc_enc(title_key, iv, apps+len_apps-temp2, round_up(temp, 0x10), apps+len_apps-temp2);
		}

		// Sign
		TMD_resign(tmd, len_tmd_nb);
		u8 *header = (u8*)malloc(0x40);
		memset(header, 0, 0x40);

		wbe32(header, 0x20); // Header size
		wbe32(header + 0x4, 0x49730000); // Header type
		wbe32(header + 0x8, len_cert_nb); // Cert length
		wbe32(header + 0xC, 0x00000000);
		wbe32(header + 0x10, len_tik_nb); // Ticket length
		wbe32(header + 0x14, len_tmd_nb); // TMD length
		wbe32(header + 0x18, len_apps); // APP length
		wbe32(header + 0x1C, len_trailer_nb); // Trailer length


		//Write wad file
		fapp = fopen(targetPath, "wb");
		if (!fapp) {
			return -9;
		}
		fwrite(header, 0x40, 1, fapp);
		fwrite(cert, len_cert, 1, fapp);
		fwrite(tik, len_tik, 1, fapp);
		fwrite(tmd, len_tmd, 1, fapp);
		fwrite(apps, len_apps, 1, fapp);
		fwrite(trailer, len_trailer, 1, fapp);
		fclose(fapp);
		return 0;
	}

	static FILE* fp;
	//outputFolder: folder without ending \
	//certFilename: filename to use create the cert file as
	//tikFilename: filename to use create the tik file as
	//tmdFilename: filename to use create the tmd file as
	//trailerFilename: filename to use create the trailer file as
	//Return values:
	// 0: Success
	// -1: Cannot open input WAD file
	// -2: Error reading WAD header
	// -3: Error reading WAD header, secondary
	// -4: Error, unknown header type
	// -5: Error unexpected end of header file.
	// -6: Error, WAD header too big
	// -100 -x: see errors from unpackWadInternal
	// -200 -x: see errors from get_appfile
	__declspec(dllexport) int unpackWad(char* pathToWad, char* pathToCommonKey, char* certFilename, char* tikFilename, char* tmdFilename, char* trailerFilename, char* outputFolder)
	{
		fp = fopen(pathToWad, "rb");
		if (!fp) 
		{
			return -1;//printf("Cannot open file %s.\n", argv[1]);
		}
		while (!feof(fp))
		{
			u8 header[0x80];
			u32 header_len;
			u32 header_type;

			if (fread(header, 0x40, 1, fp) != 1) {
				if (!feof(fp))
				{
					fclose(fp);
					return -2;//fatal("reading wad header");
				}
				else
				{
					fclose(fp);
					return 0;
				}
			}
			header_len = be32(header);
			if (header_len >= 0x80)
			{
				fclose(fp);
				return -6;//ERROR("wad header too big\n");
			}
			if (header_len >= 0x40)
				if (fread(header + 0x40, 0x40, 1, fp) != 1)
				{
					fclose(fp);
					return -3;//fatal("reading wad header (2)");
				}

			header_type = be32(header + 4);
			int unpackResult=-200;
			switch (header_type) 
			{
				case 0x49730000:
					unpackResult = unpackWadInternal(header, outputFolder, pathToCommonKey, certFilename,tikFilename,tmdFilename,trailerFilename);
					if(unpackResult!=0)
					{
						fclose(fp);
						return unpackResult-100;
					}
					break;
				case 0x69620000:
					unpackResult = unpackWadInternal(header, outputFolder, pathToCommonKey, certFilename,tikFilename,tmdFilename,trailerFilename);
					if(unpackResult!=0)
					{
						fclose(fp);
						return unpackResult-100;
					}
					break;
				default:
					fclose(fp);
					return -4;//fatal("unknown header type %08x", header_type);
			}
		}
		fclose(fp);
	}
	// Return codes:
	// 0: Success
	// -1: Bad header length
	// -7: Error changing directory to ouput folder
	// -8: Error reading cert, tik, tmd, app, or trailer file
	// -9: Error opening cert file for writing.
	// -10: Error opening trailer file for writing.
	// -11: Error opening tmd file for writing.
	// -12: Error opening tik file for writing.
	// -100-x: See errors from get_appfile
	int unpackWadInternal(u8* header, char* outputFolder, char* pathToCommonKey, char* certFilename, char* tikFilename, char* tmdFilename, char* trailerFilename)
	{
		u32 header_len;
		u32 cert_len;
		u32 tik_len;
		u32 tmd_len;
		u32 app_len;
		u32 trailer_len;
		u8 *cert;
		u8 *tik;
		u8 *tmd;
		u8 *app;
		u8 *trailer;
		u32 ret;
		//char name[256];

		header_len = be32(header);
		if (header_len != 0x20)
			return -1;//fatal("bad install header length (%x)", header_len);
		cert_len = be32(header + 8);
		// 0 = be32(header + 0x0c);
		tik_len = be32(header + 0x10);
		tmd_len = be32(header + 0x14);
		app_len = be32(header + 0x18);
		trailer_len = be32(header + 0x1c);
		cert = get_wad(cert_len);
		tik = get_wad(tik_len);
		tmd = get_wad(tmd_len);
		app = get_wad(app_len);
		trailer = get_wad(trailer_len);
		
		if(cert == NULL || tik == NULL || tmd == NULL|| app == NULL || trailer == NULL)
			return -8;
		// File Dump
		// Select Folder
		if(chdir(outputFolder))
			return -7;

		// File Dump
		//sprintf(name, "%016llx.cert", be64(tmd + 0x018c));
		//Cert
		FILE *cf = fopen(certFilename, "wb");
		if(!cf)
			return -9;
		long resultL =fwrite(cert, cert_len, 1, cf);
		fclose(cf);
		
		//Trailer
		if (trailer_len>0) 
		{
		//sprintf(name, "%016llx.trailer", be64(tmd + 0x018c));
			cf = fopen(trailerFilename, "wb");
			if(!cf)
				return -10;
			resultL=fwrite(trailer, trailer_len, 1, cf);
			fclose(cf);
		}

		//sprintf(name, "%016llx.tmd", be64(tmd + 0x018c));
		//TMD
		cf = fopen(tmdFilename, "wb");
		if(!cf)
			return -11;
		resultL=fwrite(tmd, tmd_len, 1, cf);
		fclose(cf);

		//sprintf(name, "%016llx.tik", be64(tmd + 0x018c));
		//Ticket
		cf = fopen(tikFilename, "wb");
		if(!cf)
			return -12;
		resultL=fwrite(tik, tik_len, 1, cf);
		fclose(cf);
		
		//app files
		int result=-100;
		result = get_appfile(app, app_len, tik, tmd, pathToCommonKey);
		if(result!=0)
			return result-100;
		return 0;
	}
	//Return values
	// NULL if an error occurred
	// non-NULL if correct value
	u8* get_wad(u32 len)
	{
		u32 rounded_len;
		u8 *p;

		rounded_len = round_up(len, 0x40);
		p = (u8*)malloc(rounded_len);
		if (p == 0)
			return NULL;//fatal("malloc");
		if (len)
			if (fread(p, rounded_len, 1, fp) != 1)
				return NULL;//fatal("get_wad read, len = %x", len);
		return p;
	}

	//Return values
	// 0: Success
	// -1: Error opening key file
	// -2: Error reading key file
	// -3: Error opening app file for writing.
	// -4: Error writing to app file.
	int get_appfile(u8 *app, u32 app_len, u8 *tik, u8 *tmd, char* pathToCommonKey)
	{
		u8 title_key[16];
		u8 iv[16];
		u32 i;
		u8 *p;
		u32 len;
		u32 rounded_len;
		u32 num_contents;
		u32 cid;
		u16 index;
		u16 type;
		char name[17];
		FILE *fp;

		int result = decrypt_title_key(tik, title_key, pathToCommonKey);
		if(result!=0)
			return result;

		num_contents = be16(tmd + 0x01de);
		p = app;

		for (i = 0; i < num_contents; i++) 
		{
			cid = be32(tmd + 0x01e4 + 0x24*i);
			index = be16(tmd + 0x01e8 + 0x24*i);
			type = be16(tmd + 0x01ea + 0x24*i);
			len = be64(tmd + 0x01ec + 0x24*i);
			rounded_len = round_up(len, 0x40);

			memset(iv, 0, sizeof iv);
			memcpy(iv, tmd + 0x01e8 + 0x24*i, 2);
			aes_cbc_dec(title_key, iv, p, rounded_len, p);

			sprintf(name, "%08x.app", index);
			fp = fopen(name, "wb");
			if (fp == 0)
				return -3;//fatal("open %s", name);
			if (fwrite(p, len, 1, fp) != 1)
				return -4;//fatal("write %s", name);
			fclose(fp);

			p += rounded_len;
		}
		return 0;
	}

}