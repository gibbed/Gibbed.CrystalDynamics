#include <windows.h>

HINSTANCE gHInstance = NULL;

void * __stdcall GetDFEngine(void)
{
	return NULL;
}

BOOL WINAPI DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
	switch (fdwReason)
	{
		case DLL_PROCESS_ATTACH:
		{
			gHInstance = hinstDLL;
			break;
		}
	}

	return TRUE;
}
