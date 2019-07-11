#include <iostream>
#include <string>
#include <vector>
#include <stdio.h> //printf()
#include "dictionnaire.hpp"
using namespace std;

class Dictionnaire
{
    public:
    //S'il y a eu une entrée dans mot, alors il renvoie cette valeur après avoir spécifié l'emplacement dans le dictionnaire
    string retourneMot(int emplacement)
    {
        if (emplacement < 0)
            return "NOT_DEFINED";
        if (mot.size() >= (unsigned) emplacement)
            return mot[emplacement];
        else
            return "NOT_DEFINED";
    }

    //Ajout d'une nouvelle définition. Le mot ne doit pas être vide
    bool ajouter(string aMot, string aDefinition)
    {
        if (aMot != "")
        {
            mot.push_back(aMot);
            definition.push_back(aDefinition);
            return true;
        }
        else
            return false;
    }
    //On demande le mot à chercher et l'adresse du nombre qui représente la position dans le dictionnaire. Si il est présent, alors on change la valeur qui pointe la position
    //Sinon, on retourne false pour erreur
    bool existe(string eMot, int *pPosition)
    {
        unsigned int temp = 0;
        while (eMot != mot[temp] || temp < mot.size())
        {
            if (eMot == mot[temp])
            {
                *pPosition = temp;
                return true;
            }
            temp++;
        }
        return false;
    }
    //Si on veut retirer un mot du dictionnaire
    bool retirer(string rMot)
    {
        int position = 0;
        bool ex;
        ex = existe(rMot, &position);
        if (ex == true)
        {
            mot.erase(mot.begin()+position);
            definition.erase(definition.begin()+position);
        }
        return false;
    }
    //Si on veut retourner la définition d'un mot. En entrée, on demande le mot à chercher
    string retourneDef (string rMot)
    {
        int position = 0;
        bool ex = existe(rMot, &position);
        if (ex == true)
            return definition[position];
        else
            return "NOT_DEFINED";
    }
    //Retourne la taille du dictionnaire
    unsigned int tailleDictionnaire()
    {
        //cout<<mot.max_size()<<" "<<definition.max_size()<<endl;
        return mot.size();
    }

    private:
    vector<string> mot;
    vector<string> definition;
};
