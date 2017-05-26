//#include "WbfsInterm.h"
//#include <iostream>
//
//void __stdcall progress_callbacker (int status,int total)
//{
//	if(status%10==0)
//		cout<<"Status: "<<status<<" Total: "<<total<<endl;
//	return;
//}
//void __stdcall error_callbacker(const char* message)
//{
//	cerr<<endl<<"ERROR!!! "<<message<<endl;
//}
//void main()
//{
//	OpenDrive("S");
//	//SubscribeErrorEvent(error_callbacker);
//	int y;
//	int  res = DriveToDriveSingleCopy("I", progress_callbacker, "REXE01");
//	//char* discId=new char[1], *discName=new char[1];
//	//unsigned int* size=new unsigned int;
////	//float* realsize=new float;
////	//int result=-1000;
////	////result=GetDiscInfo(0, discId,realsize,discName);
////	////#define GB (1024 * 1024 * 1024.)
////	////unsigned int sz=*size;
////	////float realsize=sz* 4ULL / (GB);
////	////cout<<discId<<" "<<(*realsize)<<" "<<discName<<" "<<endl;
////	////RenameDiscOnDrive(discId, "Wii Sports");
////	////GetDiscInfo(0, discId,size,discName);
////	////cout<<discId<<" "<<size<<" "<<discName<<" "<<endl;
////	////cout<<discId<<" "<<(*realsize)<<" "<<discName<<" "<<endl;
////	////cout<<" "<<x<<endl;
////	///*cout<<"Disc count: "<<GetDiscCount()<<endl;
////	//cout<<"Used block count: "<<GetUsedBlocksCount()<<endl;
////	//unsigned int *blocks=new unsigned int;
////	//float *total=new float,*used=new float,*free=new float;
////	//GetDriveStats(blocks,total,used,free);
////	//cout<<"Drive stats: "<<(*blocks);
////	//cout<<" ";
////	//cout<<(*total);
////	//cout<<" "<<(*used);
////	//cout<<" "<<(*free);*/
////	////result = ExtractDiscFromDrive(discId, progress_callbacker,"WiiSports.iso");
////	////result=RemoveDiscFromDrive(discId);
//	
//	
//	CloseDrive();
//
//
////	//FormatDrive("I");
////	//delete discId,discName,size,blocks,total,used,free;
////	
////	/*unsigned int *blocks=new unsigned int;
////	float *total=new float,*used=new float,*free=new float;
////	GetDriveStats(blocks,total,used,free);
////	cout<<"Drive stats: "<<(*blocks);
////	cout<<" ";
////	cout<<(*total);
////	cout<<" "<<(*used);
////	cout<<" "<<(*free);*/
//
//
//	//char y;
//	cout<<res;
//	cin>>y;
//}
//
//
////Wii Sports Sector: 50, 464, 768
//
