#if defined (WIN32)
    #include <winsock2.h>
    typedef int socklen_t;
#elif defined (linux)
    #include <sys/types.h>
    #include <sys/socket.h>
    #include <netinet/in.h>
    #include <arpa/inet.h>
    #include <unistd.h>
    #define INVALID_SOCKET -1
    #define SOCKET_ERROR -1
    #define closesocket(s) close(s)
    typedef int SOCKET;
    typedef struct sockaddr_in SOCKADDR_IN;
    typedef struct sockaddr SOCKADDR;
#endif

#include <stdio.h>
#include <stdlib.h>
#include <iostream>
#define PORT 50000

using namespace std;

int main(void)
{
    #if defined (WIN32)
        WSADATA WSAData;
        int erreur = WSAStartup(MAKEWORD(2,2), &WSAData);
    #else
        int erreur = 0;
    #endif

    SOCKET sock;
    SOCKADDR_IN sin;
    char buffer[512] = "";

    /* Si les sockets Windows fonctionnent */
    if(!erreur)
    {
        /* Création de la socket */
        sock = socket(AF_INET, SOCK_STREAM, 0);

        /* Configuration de la connexion */
        sin.sin_addr.s_addr = inet_addr("127.0.0.1");
        sin.sin_family = AF_INET;
        sin.sin_port = htons(PORT);

        /* Si l'on a réussi à se connecter */
        if(connect(sock, (SOCKADDR*)&sin, sizeof(sin)) != SOCKET_ERROR)
        {
            printf("Connection à %s sur le port %d\n", inet_ntoa(sin.sin_addr), htons(sin.sin_port));

            while (buffer != "fin")
            {
                //cin.ingore();
                //getline(cin, buffer);
                cin.getline(buffer, 512);
                send(sock, buffer, 512, 0);
                /* Si l'on reçoit des informations : on les affiche à l'écran */
                if(recv(sock, buffer, 512, 0) != SOCKET_ERROR)
                    printf("Recu : %s\n", buffer);
            }
        }
        /* sinon, on affiche "Impossible de se connecter" */
        else
        {
            printf("Impossible de se connecter\n");
        }

        /* On ferme la socket */
        closesocket(sock);

        #if defined (WIN32)
            WSACleanup();
        #endif
    }

    /* On attend que l'utilisateur tape sur une touche, puis on ferme */
    getchar();

    return EXIT_SUCCESS;
}
